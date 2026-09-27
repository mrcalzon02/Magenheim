param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$output = Join-Path $PSScriptRoot '../assets/loading/underworld'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sourceImage = [Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $Source))
try {
    if ($sourceImage.Width -ne 1672 -or $sourceImage.Height -ne 941) { throw 'Expected the supplied 1672x941 ten-panel artwork.' }
    # Pixel bounds exclude the pale horizontal and vertical separators, without resampling.
    $rows = @(@(0,189), @(194,182), @(382,172), @(560,177), @(743,198))
    $names = @('root-caverns','burning-roots','sulfurous-wastes','frozen-caverns','fungal-forest','great-decay','ancient-ruins','fracture-zones','blackwater-deep','titanbone-arches')
    for ($row=0; $row -lt 5; $row++) {
        for ($column=0; $column -lt 2; $column++) {
            $x = if ($column -eq 0) { 0 } else { 840 }
            $width = 832
            $rect = [Drawing.Rectangle]::new($x,$rows[$row][0],$width,$rows[$row][1])
            $panel = $sourceImage.Clone($rect,[Drawing.Imaging.PixelFormat]::Format24bppRgb)
            try { $panel.Save((Join-Path $output ($names[$row*2+$column]+'.png')),[Drawing.Imaging.ImageFormat]::Png) }
            finally { $panel.Dispose() }
        }
    }
} finally { $sourceImage.Dispose() }
