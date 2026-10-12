using System.IO;
using System.Text.Json;

namespace iPhoneDesk;

internal static class DiagnosticSelfTest
{
    // File-only tests: no BLE services, UI hooks, receiver, or phone commands.
    public static int Run()
    {
        string dir=Path.Combine(AppContext.BaseDirectory,"diagnostic-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var logger=new GestureDiagnostics(dir);
            logger.Event("physical-input",7,new {sample=1,x=100,y=32767});
            logger.Event("coalesced",7,new {replacedSample=1,keptSample=2});
            logger.Event("send-end",7,new {sample=2,positionReports=1,buttonReports=0});
            if(!logger.Complete(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult() || logger.WriteError!=null || logger.Dropped!=0)return 61;
            var rows=File.ReadAllLines(logger.FilePath).Select(line=>JsonDocument.Parse(line)).ToArray();
            try
            {
                if(rows.Length!=4)return 62;
                for(int i=0;i<3;i++)
                {
                    var r=rows[i].RootElement;
                    if(r.GetProperty("seq").GetInt64()!=i+1 || r.GetProperty("gesture").GetInt64()!=7 ||
                        r.GetProperty("session").GetString()!=logger.Session || r.GetProperty("executionVerified").GetBoolean())return 63;
                }
                if(rows[1].RootElement.GetProperty("data").GetProperty("replacedSample").GetInt64()!=1 ||
                    rows[2].RootElement.GetProperty("data").GetProperty("sample").GetInt64()!=2 ||
                    rows[3].RootElement.GetProperty("kind").GetString()!="log-close")return 64;
            }
            finally{foreach(var row in rows)row.Dispose();}
            logger.Event("after-close",8,new {});
            if(logger.Dropped!=1)return 65;
            var concurrent=new GestureDiagnostics(dir);
            Parallel.For(0,64,i=>concurrent.Event("concurrent",10,new {sample=i}));
            if(!concurrent.Complete(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult() || concurrent.Dropped!=0)return 68;
            var parallelRows=File.ReadAllLines(concurrent.FilePath);
            if(parallelRows.Length!=65)return 69;
            for(int i=0;i<64;i++)
            {
                using var row=JsonDocument.Parse(parallelRows[i]);
                if(row.RootElement.GetProperty("seq").GetInt64()!=i+1)return 70;
            }
            string blocked=Path.Combine(dir,"not-a-directory");File.WriteAllText(blocked,"test");
            var broken=new GestureDiagnostics(blocked);
            broken.Event("write-failure",9,new {});
            if(!broken.Complete(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult() || broken.WriteError==null)return 66;
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"diagnostic-self-test.txt"),"PASS ordered correlation, coalescing evidence, drain, closed/drop count, explicit disk failure; no phone actions\n");
            return 0;
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"diagnostic-self-test.txt"),ex.ToString());return 67;}
    }
}
