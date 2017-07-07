function Print-Header($title) {
    Write-Host ""
    Write-Host "$title" -f Cyan
    Write-Host "$("-" * 80)" -f Yellow    
}

$ErrorActionPreference = "Stop"

# -----------------------------------------------------------------------------
#
# Detect build version
#
# -----------------------------------------------------------------------------
Print-Header "Detecting version number"

if($env:APPVEYOR) {
    $VERSION = $env:APPVEYOR_BUILD_VERSION
} else {
    Write-Host "Build is not running on AppVeyor, will use default version number v1.0.0"
    $VERSION = "1.0.0"
}

Write-Host "Version number: " -n
Write-Host "$VERSION" -f yellow

# -----------------------------------------------------------------------------
#
# Restore nuget package dependencies
#
# -----------------------------------------------------------------------------
Print-Header "Restoring dependencies"
& dotnet restore /nologo -v q
if($LASTEXITCODE -ne 0) {
    Write-Host "dotnet restore failed with $LASTEXITCODE"
    exit $LASTEXITCODE
}

# -----------------------------------------------------------------------------
#
# Compile projects
#
# -----------------------------------------------------------------------------
Print-Header "Compiling"
& dotnet build /nologo -v q -c Release /p:Version=$VERSION
if($LASTEXITCODE -ne 0) {
    Write-Host "dotnet build failed with $LASTEXITCODE"
    exit $LASTEXITCODE
}

# -----------------------------------------------------------------------------
#
# Create nuget packages
#
# -----------------------------------------------------------------------------
Print-Header "Packaging artifacts"
$ARTIFACTS = Join-Path (Resolve-Path ".") "artifacts"
if(-not (Test-Path $ARTIFACTS)) {
    New-Item -Path $ARTIFACTS -ItemType Directory | Out-Null
}

Get-ChildItem $ARTIFACTS | Remove-Item -Recurse -Force

& dotnet pack /nologo -v q -c Release /p:Version=$VERSION --include-symbols --include-source --output $ARTIFACTS
if($LASTEXITCODE -ne 0) {
    Write-Host "dotnet pack failed with $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Completed" -f Green