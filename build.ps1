param(
    [string] $Configuration = 'Release',
		[string] $Platform = 'win-x64',
    [bool] $VersioningEnabled = $true,
    [string] $BuildVerbosity = 'normal'
)

$ErrorActionPreference = "Stop";
function Check-LastExitCode() {
    if ($LastExitCode -ne 0) { exit $LastExitCode }
}

if ($VersioningEnabled) {
    $VersionData = &gitversion /nofetch | Out-String | ConvertFrom-Json
    Check-LastExitCode
    $VersionProperties = "-p:Version={0};PackageVersion={1};AssemblyInformationalVersion={2}" -f $VersionData.AssemblySemVer, $VersionData.NuGetVersion, $VersionData.InformationalVersion
}

if (Test-Path dist) { Remove-Item dist -Recurse }
New-Item dist -ItemType Directory

dotnet restore --interactive
Check-LastExitCode
dotnet publish -r win-x64 -p:Configuration=$Configuration -p:PublishSingleFile=true --self-contained true $VersionProperties -v:$BuildVerbosity
Check-LastExitCode

Copy-Item -Recurse "$pwd\bin\$Configuration\net5\$Platform\publish" -Destination $pwd\dist\AnalyzeDotNetProject-$Platform\
    