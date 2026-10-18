param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDirectory,

    [ValidateSet('Debug', 'Staging', 'Release')]
    [string]$Configuration = 'Release',

    [string]$ExpectedVersion = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force
if (-not [System.IO.Path]::IsPathRooted($ArtifactDirectory)) {
    $ArtifactDirectory = Join-Path $repositoryRoot $ArtifactDirectory
}
$ArtifactDirectory = [System.IO.Path]::GetFullPath($ArtifactDirectory)
$projectPath = Join-Path $repositoryRoot 'Icod.Dedit.csproj'
$version = Get-MSBuildProperty -ProjectPath $projectPath -Name 'PackageVersion' -Configuration $Configuration
if ([string]::IsNullOrWhiteSpace($version)) { throw 'Icod.Dedit.csproj must declare PackageVersion.' }
if ($ExpectedVersion -and $version -ne $ExpectedVersion) {
    throw "Project package version '$version' differs from release tag '$ExpectedVersion'."
}
$packages = @(Get-ChildItem -LiteralPath $ArtifactDirectory -Filter '*.nupkg' -File |
    Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [System.StringComparison]::OrdinalIgnoreCase) })
if (1 -ne $packages.Count -or $packages[0].Name -ne "Icod.Dedit.$version.nupkg") {
    throw "Expected exactly one package named 'Icod.Dedit.$version.nupkg' in '$ArtifactDirectory'."
}
$package = $packages[0]
$packageMetadata = Get-PackageMetadata -PackagePath $package.FullName
if ($packageMetadata.Id -ne 'Icod.Dedit' -or $packageMetadata.Version -ne $version) {
    throw "Tool package identity '$($packageMetadata.Id) $($packageMetadata.Version)' does not match Icod.Dedit $version."
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $nuspecEntry = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) })[0]
    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    $license = $metadata.SelectSingleNode("*[local-name()='license']")
    if ($null -eq $license -or $null -eq $license.Attributes['type'] -or
        $license.Attributes['type'].Value -ne 'expression' -or $license.InnerText.Trim() -ne 'GPL-3.0-or-later') {
        throw "Tool package must declare GPL-3.0-or-later as its license expression."
    }
    foreach ($supportFile in @('README.md', 'CHANGELOG.md', 'LICENSE')) {
        $entry = $archive.GetEntry($supportFile)
        if ($null -eq $entry) { throw "Tool package '$($package.Name)' is missing '$supportFile'." }
        $reader = [System.IO.StreamReader]::new($entry.Open())
        try { $packagedText = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $sourceText = [System.IO.File]::ReadAllText((Join-Path $repositoryRoot $supportFile))
        if (-not [string]::Equals($packagedText, $sourceText, [System.StringComparison]::Ordinal)) {
            throw "Tool package '$($package.Name)' contains a stale '$supportFile'."
        }
    }
    foreach ($framework in @('net8.0', 'net9.0', 'net10.0')) {
        $root = "tools/$framework/any"
        foreach ($relativePath in @('DotnetToolSettings.xml', 'dedit.dll', 'dedit.runtimeconfig.json', 'dedit.deps.json', 'Icod.Widgets.dll')) {
            if ("$root/$relativePath" -notin $entries) {
                throw "Tool package '$($package.Name)' is missing '$root/$relativePath'."
            }
        }
        $dependencyEntry = $archive.GetEntry("$root/dedit.deps.json")
        $reader = [System.IO.StreamReader]::new($dependencyEntry.Open())
        try { $dependencyManifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if (@($dependencyManifest.libraries.PSObject.Properties.Name) -notcontains 'Icod.Widgets/1.8.0') {
            throw "Tool payload '$framework' does not reference Icod.Widgets 1.8.0."
        }
        $settingsEntry = $archive.GetEntry("$root/DotnetToolSettings.xml")
        $reader = [System.IO.StreamReader]::new($settingsEntry.Open())
        try { [xml]$settings = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $commands = @($settings.DotNetCliTool.Commands.Command)
        if (1 -ne $commands.Count -or "$($commands[0].Name)" -ne 'dedit' -or
            "$($commands[0].Runner)" -ne 'dotnet') {
            throw "Tool settings for '$framework' must declare exactly one dedit/dotnet command."
        }
    }
    $settingsPaths = @($entries | Where-Object { $_.EndsWith('/DotnetToolSettings.xml', [System.StringComparison]::OrdinalIgnoreCase) })
    if (3 -ne $settingsPaths.Count) {
        throw "Expected exactly three tool framework settings; found $($settingsPaths.Count)."
    }
} finally {
    $archive.Dispose()
}

$validationRoot = Join-Path $repositoryRoot 'artifacts/dedit-tool-validation'
$toolPath = Join-Path $validationRoot 'tool'
$configPath = Join-Path $validationRoot 'NuGet.Config'
if (Test-Path -LiteralPath $validationRoot) { Remove-Item -LiteralPath $validationRoot -Recurse -Force }
New-Item -ItemType Directory -Path $validationRoot -Force | Out-Null
$source = [System.Security.SecurityElement]::Escape($ArtifactDirectory)
$config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /><add key="local" value="$source" /></packageSources></configuration>
"@
[System.IO.File]::WriteAllText($configPath, $config, [System.Text.UTF8Encoding]::new($false))
Invoke-DotNet -Arguments @('tool', 'install', 'Icod.Dedit', '--tool-path', $toolPath,
    '--version', $version, '--configfile', $configPath)
$executable = if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)) {
    Join-Path $toolPath 'dedit.exe'
} else {
    Join-Path $toolPath 'dedit'
}
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Tool install did not create '$executable'."
}
$versionOutput = (& $executable --version | Out-String).Trim()
if (0 -ne $LASTEXITCODE -or $versionOutput -ne "dedit $version") {
    throw "Installed dedit --version returned '$versionOutput' with status $LASTEXITCODE."
}
$helpOutput = (& $executable --help | Out-String).Trim()
if (0 -ne $LASTEXITCODE -or -not $helpOutput.StartsWith('Usage: dedit ', [System.StringComparison]::Ordinal)) {
    throw "Installed dedit --help returned unexpected output with status $LASTEXITCODE."
}
# Exercise the update command against the exact candidate package. This is a
# same-version lifecycle check, not evidence for a cross-version upgrade.
Invoke-DotNet -Arguments @('tool', 'update', 'Icod.Dedit', '--tool-path', $toolPath,
    '--version', $version, '--configfile', $configPath)
$updatedVersionOutput = (& $executable --version | Out-String).Trim()
if (0 -ne $LASTEXITCODE -or $updatedVersionOutput -ne "dedit $version") {
    throw "Updated dedit --version returned '$updatedVersionOutput' with status $LASTEXITCODE."
}

Invoke-DotNet -Arguments @('tool', 'uninstall', 'Icod.Dedit', '--tool-path', $toolPath)
if (Test-Path -LiteralPath $executable -PathType Leaf) {
    throw "Tool uninstall left the dedit command at '$executable'."
}
Write-Host "Verified install, launch, same-version update and removal of exact Icod.Dedit $version tool package ($Configuration)."
