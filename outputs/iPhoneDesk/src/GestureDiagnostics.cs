using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;

namespace iPhoneDesk;

// Diagnostics never wait in the input path. Loss and write failure are explicit.
internal sealed class GestureDiagnostics
{
    // Resource bound: at most 4096 pending records, rather than unlimited memory
    // when storage stalls. This does not limit phone input or hide any controls.
    private readonly Channel<object> records = Channel.CreateBounded<object>(
        new BoundedChannelOptions(4096) { SingleReader=true, FullMode=BoundedChannelFullMode.Wait });
    private readonly Task writer;
    private long sequence, dropped;
    private readonly object enqueueLock=new();
    private readonly long start = Stopwatch.GetTimestamp();
    public string Session { get; } = Guid.NewGuid().ToString("N");
    public string FilePath { get; }
    public string? WriteError { get; private set; }
    public long Dropped => Interlocked.Read(ref dropped);

    public GestureDiagnostics(string directory)
    {
        FilePath=Path.Combine(directory,$"gesture-{DateTime.Now:yyyyMMdd-HHmmss}-{Session}.jsonl");
        writer=Task.Run(WriteRecords);
    }
    public void Event(string kind,long gesture,object data)
    {
        // Keep sequence and enqueue order consistent across HID/startup threads.
        // The lock covers only an in-memory TryWrite; never I/O or awaiting space.
        lock(enqueueLock)
        {
            var record=new {schema=1, session=Session, seq=++sequence,
                at=DateTimeOffset.Now, elapsedMs=Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                kind, gesture, executionVerified=false, data};
            if(!records.Writer.TryWrite(record))Interlocked.Increment(ref dropped);
        }
    }
    private async Task WriteRecords()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            using var output=new StreamWriter(new FileStream(FilePath,FileMode.CreateNew,
                FileAccess.Write,FileShare.Read,4096,FileOptions.Asynchronous));
            await foreach(var record in records.Reader.ReadAllAsync())
            {
                await output.WriteLineAsync(JsonSerializer.Serialize(record));
                // Flush makes the latest completed record available while running.
                await output.FlushAsync();
            }
            await output.WriteLineAsync(JsonSerializer.Serialize(new {schema=1,session=Session,
                kind="log-close",dropped=Dropped,executionVerified=false}));
        }
        catch(Exception ex)
        {
            WriteError=$"{ex.GetType().Name}: {ex.Message}";
            // Reject future records and drain pending objects after a disk failure.
            records.Writer.TryComplete();
            while(records.Reader.TryRead(out _))Interlocked.Increment(ref dropped);
        }
    }
    public async Task<bool> Complete(TimeSpan timeout)
    {
        records.Writer.TryComplete();
        return await Task.WhenAny(writer,Task.Delay(timeout))==writer;
    }
}
