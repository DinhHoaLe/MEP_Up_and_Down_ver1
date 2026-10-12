param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
# The add-in targets x64; re-run in native PowerShell when launched from a 32-bit host.
if ([IntPtr]::Size -eq 4) {
 $nativePowerShell = Join-Path $env:WINDIR 'Sysnative\WindowsPowerShell\v1.0\powershell.exe'
 $nativeArguments = @('-NoProfile','-NonInteractive','-STA','-ExecutionPolicy','Bypass','-File',$PSCommandPath)
 if ($OutputDirectory) { $nativeArguments += @('-OutputDirectory',$OutputDirectory) }
 & $nativePowerShell @nativeArguments
 exit $LASTEXITCODE
}
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, System.Xaml
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'Resources\Symbols' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$source = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $projectRoot 'UpDownTool\MEPSymbols.cs')
Add-Type -TypeDefinition $source -ReferencedAssemblies @('PresentationCore','PresentationFramework','WindowsBase','System.Xaml')
function Save-Png($visual, $width, $height, $path) {
 $bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap($width, $height, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
 if ([IO.Path]::GetFileName($path).StartsWith('UI-Preview')) {
  $background = New-Object Windows.Media.DrawingVisual
  $bgContext = $background.RenderOpen()
  $bgContext.DrawRectangle([Windows.Media.BrushConverter]::new().ConvertFromString('#F5F2EE'),$null,(New-Object Windows.Rect(0,0,$width,$height)))
  $bgContext.Close()
  $bitmap.Render($background)
 }
 $bitmap.Render($visual)
 $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
 $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream = [IO.File]::Create($path)
 try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
$kinds = @('Duct','Pipe','Cable Tray','Conduit')
$options = @('Cut Up','Cut Up','Cut Down','Cut Down','Move Up','Move Down')
$angles = @($true,$false,$true,$false,$false,$false)
$sheet = New-Object Windows.Media.DrawingVisual
$context = $sheet.RenderOpen()
$context.DrawRectangle([Windows.Media.Brushes]::White, $null, (New-Object Windows.Rect(0,0,1440,1440)))
for ($row=0; $row -lt 6; $row++) {
 for ($col=0; $col -lt 4; $col++) {
  $kind = $kinds[$col]; $option = $options[$row]; $angle = $angles[$row]
  $symbol = [MEP_Up_and_Down.UpDownTool.MEPSymbols]::Create($kind,$option,$angle,'1000')
  $title = $kind + ' / ' + $option
  $suffix = if ($row -lt 4) { if ($angle) { '-45' } else { '-90' } } else { '' }
  $name = ($kind + '-' + $option).Replace(' ','-') + $suffix
  $visual = New-Object Windows.Media.DrawingVisual
  $dc = $visual.RenderOpen()
  $dc.DrawImage($symbol,(New-Object Windows.Rect(0,0,360,190)))
  $dc.Close()
  Save-Png $visual 360 190 (Join-Path $OutputDirectory ($name + '.png'))
  [IO.File]::WriteAllText((Join-Path $OutputDirectory ($name + '.xaml')),[Windows.Markup.XamlWriter]::Save($symbol))
  $context.DrawImage($symbol,(New-Object Windows.Rect(($col*360),($row*240+35),360,190)))
  $caption = New-Object Windows.Media.FormattedText(($title + $suffix),[Globalization.CultureInfo]::InvariantCulture,[Windows.FlowDirection]::LeftToRight,(New-Object Windows.Media.Typeface('Segoe UI')),15,[Windows.Media.Brushes]::Black,1)
  $context.DrawText($caption,(New-Object Windows.Point(($col*360+15),($row*240+10))))
 }
}
$context.Close()
Save-Png $sheet 1440 1440 (Join-Path $OutputDirectory 'Symbol-Sheet.png')
Write-Output 'Exported 24 vector XAML symbols, 24 PNG symbols and Symbol-Sheet.png.'

# Render the production layout without invoking any Revit API or showing a window.
[xml]$layout = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $projectRoot 'UpDownTool\UpAndDown.xaml')
foreach ($element in $layout.SelectNodes('//*')) {
 foreach ($attribute in @($element.Attributes)) {
  if ($attribute.Name -eq 'x:Class' -or $attribute.Name -in @('Closed','StateChanged','Click','SelectionChanged','TextChanged')) {
   $element.RemoveAttributeNode($attribute) | Out-Null
  }
 }
}
# Load the built assembly so preview images use the same embedded pack resources as Revit.
$assemblyPath = Join-Path $projectRoot 'bin\Release\MEP Up and Down.dll'
if (!(Test-Path -LiteralPath $assemblyPath)) { throw 'Build Release before exporting the UI preview.' }
[Reflection.Assembly]::LoadFrom($assemblyPath) | Out-Null
$reader = New-Object Xml.XmlNodeReader($layout)
$window = [Windows.Markup.XamlReader]::Load($reader)
$window.FindName('cbbOption').ItemsSource = @('Cut Up','Cut Down','Move Up','Move Down')
$window.FindName('cbbOption').SelectedIndex = 0
$window.FindName('cbbAngle').ItemsSource = @('45°','90°')
$window.FindName('cbbAngle').SelectedIndex = 0
$window.FindName('previewTitle').Text = 'Duct · Cut Up · 45°'
$window.FindName('previewLegend').Text = 'Dashed = existing route · Color = result · Arrow = vertical offset'
$window.FindName('hint').Text = 'Pick two points on one horizontal element. Compatible elbow fittings must be configured. Preview is schematic.'
$window.FindName('img').Source = [MEP_Up_and_Down.UpDownTool.MEPSymbols]::Create('Duct','Cut Up',$true,'1000')
$content = $window.Content
$previewWidth = [int]$window.Width
$previewHeight = [int]$window.Height
$content.Measure((New-Object Windows.Size($previewWidth,$previewHeight)))
$content.Arrange((New-Object Windows.Rect(0,0,$previewWidth,$previewHeight)))
$content.UpdateLayout()
Save-Png $content $previewWidth $previewHeight (Join-Path $OutputDirectory 'UI-Preview.png')

# Also check the smallest supported window size.
$minimumWidth = [int]$window.MinWidth
$minimumHeight = [int]$window.MinHeight
$content.Measure((New-Object Windows.Size($minimumWidth,$minimumHeight)))
$content.Arrange((New-Object Windows.Rect(0,0,$minimumWidth,$minimumHeight)))
$content.UpdateLayout()
Save-Png $content $minimumWidth $minimumHeight (Join-Path $OutputDirectory 'UI-Preview-Minimum.png')
