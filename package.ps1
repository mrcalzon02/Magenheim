param([string]$Author = 'Local', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
if ($Author -notmatch '^[A-Za-z0-9_]+$') { throw 'Author must contain only letters, digits or underscores' }
if (!$SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1') }
$manifest = Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$constant = Get-Content (Join-Path $PSScriptRoot 'src\Magenheim\Bootstrap\PluginConstants.cs') -Raw
if (!$constant.Contains('Version = "' + $manifest.version_number + '"')) { throw 'Manifest and plugin versions differ' }
$zipPath = Join-Path $PSScriptRoot "dist\$Author-Magenheim-$($manifest.version_number).zip"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.Drawing
# Simple original development monogram. This is package identification, not final game artwork.
$bitmap = New-Object System.Drawing.Bitmap 256,256
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([System.Drawing.Color]::FromArgb(19,31,28))
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(135,210,157)),8
$graphics.DrawPolygon($pen, [System.Drawing.Point[]]@((New-Object System.Drawing.Point 128,20),(New-Object System.Drawing.Point 220,82),(New-Object System.Drawing.Point 197,191),(New-Object System.Drawing.Point 128,236),(New-Object System.Drawing.Point 59,191),(New-Object System.Drawing.Point 36,82)))
$font = New-Object System.Drawing.Font 'Georgia',90,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(225,237,215))
$graphics.DrawString('M',$font,$brush,75,69)
$icon = Join-Path $PSScriptRoot 'icon.png'
$bitmap.Save($icon,[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose(); $pen.Dispose(); $brush.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
$entries = @{}
foreach($name in @('manifest.json','README.md','CHANGELOG.md','COMPATIBILITY.md','INSTALL.md','icon.png')) { $entries[$name] = Join-Path $PSScriptRoot $name }
$plugin = Join-Path $PSScriptRoot 'dist\BepInEx\plugins\Magenheim'
foreach($file in Get-ChildItem -LiteralPath $plugin -Recurse -File) {
    $relative = $file.FullName.Substring($plugin.Length + 1).Replace('\','/')
    $entries['BepInEx/plugins/Magenheim/' + $relative] = $file.FullName
}
$stream = [System.IO.File]::Open($zipPath,[System.IO.FileMode]::Create)
$zip = New-Object System.IO.Compression.ZipArchive $stream,([System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($name in ($entries.Keys | Sort-Object)) {
        $entry = $zip.CreateEntry($name)
        $target = $entry.Open()
        try { $bytes = [System.IO.File]::ReadAllBytes($entries[$name]); $target.Write($bytes,0,$bytes.Length) } finally { $target.Dispose() }
    }
} finally { $zip.Dispose(); $stream.Dispose() }
$check = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if($check.Entries.Count -ne 9) { throw 'Unexpected package file count' }
    if(@($check.Entries | Where-Object FullName -Match 'Newtonsoft|Cecil|Tests|compat-repair').Count) { throw 'Non-package library included' }
} finally { $check.Dispose() }
Write-Output "Validated local-import package: $zipPath"
