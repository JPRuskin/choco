<#
    .Synopsis
        Builds the Chocolatey MSI Installer

    .Example
        .\build.ps1 -Version 0.12.1
#>
param(
    $Version
)

Push-Location $PSScriptRoot
try {
    Invoke-WebRequest -Uri "https://community.chocolatey.org/install.ps1" -OutFile "$PSScriptRoot\Install.ps1"
    # Nuget restore WiX
    # Download Chocolatey Nupkg

    msbuild /p:DisplayVersion="$($Version)" /p:nupkg="$($Nupkg)"
} finally {
    Pop-Location
}