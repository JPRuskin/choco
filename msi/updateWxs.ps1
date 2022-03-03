<#
    .Synopsis
        Updates the files referenced within the WiX MSI definition

    .Example
        .\updateWxs.ps1 -ChocolateyPath .\built\artifacts\tools\chocolateyInstall\
#>
param(
    [ValidateScript({
        (Test-Path $_) -and
        ([xml](Get-Content $_))
    })]
    [string]$WxsPath = (Join-Path $PSScriptRoot "chocolatey.wxs"),
    
    [Parameter(Mandatory, ParameterSetName="Folder")]
    $ChocolateyPath,

    [Parameter(Mandatory, ParameterSetName="Files", ValueFromPipeline)]
    [Alias('PSPath')]
    [IO.FileInfo]$File = (Get-ChildItem $ChocolateyPath -Recurse)
)
begin {
    [xml]$WXS = Get-Content $WxsPath
}
process {
    foreach ($File in $File) {

    }
}