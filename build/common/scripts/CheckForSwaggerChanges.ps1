<#
.SYNOPSIS
Generates the OpenApi doc for the specified version and compares it with the checked in version to ensure it is up to date.
.Parameter SwaggerDir
The working directory
.PARAMETER AssemblyDir
Path for the web projects dll
.PARAMETER Versions
Api versions to compare with
.PARAMETER SwashbuckleCLIVersion
Version of SwashbuckleCLI to use
#>

param(
    [string]$SwaggerDir,

    [string]$AssemblyDir,

    [String[]]$Versions,

    [string]$SwashbuckleCLIVersion = '6.4.0'
)
$ErrorActionPreference = 'Stop'
dotnet new tool-manifest --force
dotnet tool install --version $SwashbuckleCLIVersion Swashbuckle.AspNetCore.Cli

Write-Host "Using swagger version ..."
dotnet tool list | Select-String "swashbuckle"


Write-Host "Testing that swagger will work ..."
dotnet swagger

foreach ($Version in $Versions)
{
    Write-Host "Generating swagger yaml file for $Version"
    $WritePath=(Join-Path -Path "$SwaggerDir" -ChildPath "$Version.yaml")

    dotnet swagger tofile --yaml --output $WritePath "$AssemblyDir" $Version

    Write-Host "Comparing generated swagger with what was checked in ..."
    $HasDifferences = (Compare-Object -ReferenceObject (Get-Content -Path $WritePath) -DifferenceObject (Get-Content -Path ".\swagger\$Version\swagger.yaml"))
    if ($HasDifferences){
        Write-Host $HasDifferences
        throw "The swagger yaml checked in with this PR is not up to date with code. Please build the sln, which will trigger a hook to autogenerate these files on your behalf. Differences shown above."
    }else{
        Write-Host "Swagger checked in with this PR is up to date with code."
    }


}
