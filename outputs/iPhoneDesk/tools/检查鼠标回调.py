from pathlib import Path
s=Path('outputs/iPhoneDesk/src/Program.cs').read_text(encoding='utf-8-sig')
for begin,end_marker in [('private void HoverChanged(', 'private async void ToggleCapture'),('private void QueueInput(', 'private void QueueRawInput'),('private void QueueRawInput(', 'private async void PumpInput')]:
 start=s.index(begin);end=s.index(end_marker,start);method=s[start:end]
 assert 'hid.' not in method,begin+' must not access WinRT HID members'
assert 'capture.ReleaseRequested+=()=>Dispatcher.BeginInvoke' in s,'Hook failure release must defer COM work'
assert 'connected=capture?.ClientReady' in s
logger=Path('outputs/iPhoneDesk/src/GestureDiagnostics.cs').read_text(encoding='utf-8-sig');start=logger.index('public void Event(');end=logger.index('private async Task WriteRecords',start);method=logger[start:end]
assert 'File.' not in method and 'await ' not in method and '.Wait(' not in method,'Input logging must not block on IO'
print('PASS hook callback COM boundary; input logging has no synchronous disk IO/wait')
