# Build Script for Chordality MSIX Packaging
# Targets x64 Windows 10/11 Architecture

Write-Host "Building Chordality for Release..."

# Clean old builds
dotnet clean .\Chordality.slnx -c Release

# Restore packages
dotnet restore .\Chordality.slnx

# Publish MSIX Package
# Note: Self-signed Certificate 'Chordality.pfx' must exist in Chordality.App directory.
dotnet publish .\Chordality.App\Chordality.App.csproj `
    -c Release `
    -f net8.0-windows10.0.19041.0 `
    -r win-x64 `
    /p:UapAppxPackageBuildMode=SideloadOnly `
    /p:AppxBundle=Never `
    /p:GenerateAppxPackageOnBuild=true `
    /p:AppxPackageSigningEnabled=true `
    /p:PackageCertificateKeyFile="Chordality.pfx"

Write-Host "Build complete! MSIX package is located in Chordality.App\AppPackages\"
