<#
.SYNOPSIS
Generates the OpenApi doc for the specified version and compares it with the checked in version to ensure it is up to date.
Run script from root of this repository
.Parameter SwaggerDir
Swagger directory path from root of this repository. Ex: 'swagger'
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
$container="openapitools/openapi-diff:latest@sha256:5da8291d3947414491e4c62de74f8fc1ee573a88461fb2fb09979ecb5ea5eb02"

dotnet new tool-manifest --force
dotnet tool install --version $SwashbuckleCLIVersion Swashbuckle.AspNetCore.Cli

Write-Host "Using swagger version ..."
dotnet tool list | Select-String "swashbuckle"


Write-Host "Testing that swagger will work ..."
dotnet swagger

if (Test-Path "$SwaggerDir/Ref") { Remove-Item -Recurse -Force "$SwaggerDir/Ref" }
mkdir "$SwaggerDir/Ref"

foreach ($Version in $Versions)
{
    $new=(Join-Path -Path "$SwaggerDir" -ChildPath "$Version/swagger.yaml")
    $old=(Join-Path -Path "$SwaggerDir" -ChildPath "/Ref/$Version.yaml")
    Write-Host "old: $old"
    Write-Host "new: $new"

    Write-Host "Generating swagger yaml file for $Version"
    dotnet swagger tofile --yaml --output $old "$AssemblyDir" $Version

    Write-Host "Comparing generated swagger with what was checked in ..."
    docker run --rm -t -v "${pwd}/${SwaggerDir}:/swagger:ro" $container /$old /$new --fail-on-changed
}

Remove-Item -Recurse -Force "$SwaggerDir/Ref"
