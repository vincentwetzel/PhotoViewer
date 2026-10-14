$ErrorActionPreference = 'Stop'

$workspaceRoot = $PSScriptRoot
$tempPath = Join-Path $workspaceRoot '.build\temp'

New-Item -ItemType Directory -Force -Path $tempPath | Out-Null

# Keep NuGet/MSBuild scratch files out of the restricted system TEMP directory.
$env:TEMP = $tempPath
$env:TMP = $tempPath

dotnet build (Join-Path $workspaceRoot 'PhotoViewer.sln') -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true @args
exit $LASTEXITCODE
