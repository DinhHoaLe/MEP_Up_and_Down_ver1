param([switch]$Live)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase,System.Xaml,System.Web.Extensions
[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'bin\Release\RevitCodexChat.dll')) | Out-Null
$pane = New-Object RevitCodexChat.ChatPane
$pane.SelectionCompleted("Snapshot minh họa - không phải model thật.`nPhần tử 12345: Duct`nChiều dài: 5000 mm`nKích thước: 600 x 300 mm")
$pane.Width = 390
$pane.Height = 850
$pane.Measure((New-Object Windows.Size(390,850)))
$pane.Arrange((New-Object Windows.Rect(0,0,390,850)))
$pane.UpdateLayout()
$bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap(390,850,96,96,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($pane)
$encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream = [IO.File]::Create((Join-Path $PSScriptRoot 'Preview.png'))
try { $encoder.Save($stream) } finally { $stream.Dispose() }
Write-Output 'PASS: compiled WPF pane renders with Vietnamese snapshot.'
$pane.SelectionFailed('No selection')
if ($pane.FindName('ContextPreview').Text -ne '') { throw 'Stale snapshot retained after failed read.' }
Write-Output 'PASS: failed read clears snapshot.'
$pane.Shutdown()
Write-Output ('Codex executable: ' + [RevitCodexChat.CodexClient]::FindExecutable())
if ($Live) {
    $cts = New-Object Threading.CancellationTokenSource
    $cts.CancelAfter(45000)
    try {
        $task = [RevitCodexChat.CodexClient]::AskAsync('Chỉ trả lời bằng tiếng Việt: Kết nối thử nghiệm thành công. Không dùng công cụ.', $cts.Token)
        Write-Output $task.GetAwaiter().GetResult()
    } finally { $cts.Dispose() }
    $cts = New-Object Threading.CancellationTokenSource
    $cts.CancelAfter(1000)
    try {
        $task = [RevitCodexChat.CodexClient]::AskAsync('Explain Revit element types. Do not use tools.', $cts.Token)
        try { $task.GetAwaiter().GetResult() | Out-Null; throw 'Cancellation did not stop the request.' }
        catch {
            if (!$task.IsCanceled) { throw }
            Write-Output 'PASS: cancellation terminates the active Codex request.'
        }
    } finally { $cts.Dispose() }
}
