[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $SourceDirectory,
    [Parameter(Mandatory)]
    [string] $BootstrapDirectory,
    [string] $DeliveryRoot = (Join-Path $PSScriptRoot '..\RelaxKonServer\Content\ReleaseDelivery'),
    [string] $PublicBaseUri = 'https://downloads.relaxkon.com'
)

$ErrorActionPreference = 'Stop'

function Get-FullDirectory([string] $Path, [string] $Name) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) { throw "$Name does not exist: $fullPath" }
    return $fullPath
}

function Write-Utf8Atomically([string] $Path, [string] $Content) {
    $temporaryPath = $Path + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporaryPath, $Content, [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporaryPath, $Path, $true)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
    }
}

$source = Get-FullDirectory $SourceDirectory 'SourceDirectory'
$bootstrap = Get-FullDirectory $BootstrapDirectory 'BootstrapDirectory'
$delivery = [IO.Path]::GetFullPath($DeliveryRoot)
$publicBase = $PublicBaseUri.TrimEnd('/')
if ($publicBase -notmatch '^https://[^/]+$') { throw 'PublicBaseUri must be an HTTPS origin without a path.' }

$windowsBootstrap = Join-Path $bootstrap 'Install-RelaxKonOS.ps1'
$linuxBootstrap = Join-Path $bootstrap 'install-relaxkonos.sh'
foreach ($required in @($windowsBootstrap, $linuxBootstrap)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Bootstrap directory is incomplete: $required" }
}

$published = 0
Get-ChildItem -LiteralPath $source -File -Filter '*.json' | ForEach-Object {
    $descriptorFile = $_
    $descriptor = Get-Content -LiteralPath $descriptorFile.FullName -Raw | ConvertFrom-Json
    if ($descriptor.schemaVersion -ne 1 -or $descriptor.version -notmatch '^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$' -or
        $descriptor.runtime -notin @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64') -or
        $descriptor.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $descriptor.url -notmatch '^https://') {
        throw "Invalid release descriptor: $($descriptorFile.FullName)"
    }

    $archiveName = [IO.Path]::GetFileName(([Uri] $descriptor.url).AbsolutePath)
    if ([IO.Path]::GetExtension($archiveName) -ne '.zip') { throw "Descriptor does not reference a ZIP: $($descriptorFile.FullName)" }
    $archive = Join-Path $source $archiveName
    if (-not (Test-Path -LiteralPath $archive -PathType Leaf)) { throw "Release ZIP is missing: $archive" }
    $actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not $actualHash.Equals($descriptor.sha256, [StringComparison]::OrdinalIgnoreCase)) { throw "Release ZIP checksum does not match: $archive" }

    $target = Join-Path $delivery ("relaxkonos\stable\{0}\{1}" -f $descriptor.version, $descriptor.runtime)
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $targetArchive = Join-Path $target $archiveName
    if (Test-Path -LiteralPath $targetArchive) {
        $publishedHash = (Get-FileHash -LiteralPath $targetArchive -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not $publishedHash.Equals($actualHash, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to mutate immutable versioned release: $targetArchive"
        }
    }
    else {
        Copy-Item -LiteralPath $archive -Destination $targetArchive
    }
    Write-Utf8Atomically (Join-Path $target ($archiveName + '.sha256')) "$actualHash  $archiveName`n"

    $publicDescriptor = [ordered]@{
        schemaVersion = 1
        version = [string] $descriptor.version
        runtime = [string] $descriptor.runtime
        url = "$publicBase/relaxkonos/stable/$($descriptor.version)/$($descriptor.runtime)/$archiveName"
        sha256 = $actualHash
    }
    $latest = Join-Path $delivery 'relaxkonos\stable\latest'
    New-Item -ItemType Directory -Path $latest -Force | Out-Null
    Write-Utf8Atomically (Join-Path $latest ($descriptor.runtime + '.json')) ($publicDescriptor | ConvertTo-Json)
    $published++
}

if ($published -eq 0) { throw "No release descriptors were found in $source" }
$latestBootstrap = Join-Path $delivery 'relaxkonos\stable\latest\bootstrap'
New-Item -ItemType Directory -Path $latestBootstrap -Force | Out-Null
Copy-Item -LiteralPath $windowsBootstrap -Destination (Join-Path $latestBootstrap 'Install-RelaxKonOS.ps1') -Force
Copy-Item -LiteralPath $linuxBootstrap -Destination (Join-Path $latestBootstrap 'install-relaxkonos.sh') -Force
Write-Host "Published $published runtime(s) to $delivery"
