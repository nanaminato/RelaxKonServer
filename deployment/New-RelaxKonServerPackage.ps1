[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$')]
    [string] $Version,
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64')]
    [string] $Runtime = 'linux-x64',
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',
    [string] $OutputDirectory = 'artifacts',
    [ValidatePattern('^https://[^/]+(?:/.*)?$')]
    [string] $ArtifactBaseUri = 'https://downloads.relaxkon.com/releases'
)

$ErrorActionPreference = 'Stop'

function Get-FullPath([string] $Path, [string] $BasePath) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    return [IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

$scriptDirectory = [IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $scriptDirectory '..'))
$project = Join-Path $repositoryRoot 'RelaxKonServer\RelaxKonServer.csproj'
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "Server project was not found: $project" }

# A relative output path is always resolved against RelaxKonServer, rather
# than the caller's current directory, so a release command is reproducible.
$output = Get-FullPath $OutputDirectory $repositoryRoot
$packageName = "RelaxKonServer-$Version-$Runtime"
$packagePath = Join-Path $output "$packageName.zip"
$descriptorPath = Join-Path $output "$packageName.json"
$stagingPath = Join-Path ([IO.Path]::GetTempPath()) ('RelaxKonServer-package-' + [Guid]::NewGuid().ToString('N'))

try {
    New-Item -ItemType Directory -Path $output, $stagingPath -Force | Out-Null
    & dotnet publish $project --configuration $Configuration --runtime $Runtime --self-contained true --output $stagingPath
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    $executableName = if ($Runtime.StartsWith('win-')) { 'RelaxKonServer.exe' } else { 'RelaxKonServer' }
    $executable = Join-Path $stagingPath $executableName
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Publish output is incomplete: $executable" }

    # Content is deliberately outside wwwroot, so the Web SDK does not publish
    # it automatically. It is nevertheless the API's data source at runtime.
    $contentSource = Join-Path (Split-Path -Parent $project) 'Content'
    $contentDestination = Join-Path $stagingPath 'Content'
    if (-not (Test-Path -LiteralPath $contentSource -PathType Container)) { throw "Server content directory was not found: $contentSource" }
    Remove-Item -LiteralPath $contentDestination -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item -LiteralPath $contentSource -Destination $contentDestination -Recurse -Force

    # The application resolves Content/ relative to its working directory. Keep
    # the package root as that directory when starting it under systemd or IIS.
    Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue
    Compress-Archive -LiteralPath (Get-ChildItem -LiteralPath $stagingPath -Force | Select-Object -ExpandProperty FullName) -DestinationPath $packagePath -CompressionLevel Optimal
    $sha256 = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$packagePath.sha256", "$sha256  $packageName.zip`n", [Text.UTF8Encoding]::new($false))

    $descriptor = [ordered]@{
        schemaVersion = 1
        packageKind = 'server'
        version = $Version
        runtime = $Runtime
        url = ($ArtifactBaseUri.TrimEnd('/') + '/' + $packageName + '.zip')
        sha256 = $sha256
    }
    [IO.File]::WriteAllText($descriptorPath, ($descriptor | ConvertTo-Json), [Text.UTF8Encoding]::new($false))

    Write-Host "Package: $packagePath"
    Write-Host "SHA-256: $sha256"
    Write-Host "Descriptor: $descriptorPath"
}
finally {
    Remove-Item -LiteralPath $stagingPath -Recurse -Force -ErrorAction SilentlyContinue
}
