# Renders the icon SVGs with headless Microsoft Edge and packs them into the application icon.
# Usage: powershell -ExecutionPolicy Bypass -File Branding\build_icon.ps1
#
# Size -> source:
#   16        icon_16.svg      (pixel aligned, no tile)
#   24, 32    icon_small.svg   (G only, no tile, solid color)
#   48, 64    icon_medium.svg  (tile, glass cover, G with T and N)
#   256       icon_large.svg   (tile, glass cover, library cards, glow)
# Additionally icon_large.svg is rendered as logo_1024.png (splash, installer, GitHub).

$ErrorActionPreference = 'Stop'

$brandingDir = $PSScriptRoot
$icoFile     = Join-Path $brandingDir '..\GameTimeNext\UI\Ressources\GTN_APP_ICON.ico'
$logoFile    = Join-Path $brandingDir 'logo_1024.png'

$edge = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'Microsoft Edge not found.' }

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('gtn_icon_' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $tempDir | Out-Null

function Render-Svg([string]$svgName, [int]$size, [string]$pngFile) {
    $svg = Get-Content -Raw -Path (Join-Path $brandingDir $svgName)
    # Set the output size on the root element, the viewBox stays as designed
    $svg = ([regex]'width="\d+" height="\d+"').Replace($svg, "width=`"$size`" height=`"$size`"", 1)
    $svgFile = Join-Path $tempDir "$size.svg"
    Set-Content -Path $svgFile -Value $svg -Encoding UTF8

    $url = 'file:///' + ($svgFile -replace '\\', '/')
    Start-Process -FilePath $edge -Wait -WindowStyle Hidden -ArgumentList @(
        '--headless', '--disable-gpu', '--hide-scrollbars',
        '--default-background-color=00000000',
        "--window-size=$size,$size",
        "--screenshot=`"$pngFile`"",
        "`"$url`""
    )
    if (-not (Test-Path $pngFile)) { throw "Rendering $svgName at $size px failed." }
}

$sources = @(
    @{ Size = 16;  Svg = 'icon_16.svg' },
    @{ Size = 24;  Svg = 'icon_small.svg' },
    @{ Size = 32;  Svg = 'icon_small.svg' },
    @{ Size = 48;  Svg = 'icon_medium.svg' },
    @{ Size = 64;  Svg = 'icon_medium.svg' },
    @{ Size = 256; Svg = 'icon_large.svg' }
)

try {
    $images = foreach ($source in $sources) {
        $png = Join-Path $tempDir "$($source.Size).png"
        Render-Svg $source.Svg $source.Size $png
        [pscustomobject]@{ Size = $source.Size; Bytes = [System.IO.File]::ReadAllBytes($png) }
    }

    Render-Svg 'icon_large.svg' 1024 $logoFile

    # ICO with PNG-compressed entries (supported since Windows Vista)
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$images.Count)

    $offset = 6 + 16 * $images.Count
    foreach ($img in $images) {
        $dim = if ($img.Size -ge 256) { 0 } else { $img.Size }
        $bw.Write([byte]$dim); $bw.Write([byte]$dim)
        $bw.Write([byte]0); $bw.Write([byte]0)
        $bw.Write([UInt16]1); $bw.Write([UInt16]32)
        $bw.Write([UInt32]$img.Bytes.Length); $bw.Write([UInt32]$offset)
        $offset += $img.Bytes.Length
    }
    foreach ($img in $images) { $bw.Write([byte[]]$img.Bytes, 0, $img.Bytes.Length) }
    $bw.Flush()
    [System.IO.File]::WriteAllBytes([System.IO.Path]::GetFullPath($icoFile), $ms.ToArray())

    Write-Host "Written: $([System.IO.Path]::GetFullPath($icoFile)) ($($images.Count) sizes)"
    Write-Host "Written: $logoFile"
}
finally {
    Remove-Item -Recurse -Force $tempDir
}
