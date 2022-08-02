<#
.SYNOPSIS
Generates the OpenApi doc for the specified version and compares it with the baseline to make sure no breaking changes are introduced
.Parameter SwaggerDir
The working directory
.PARAMETER AssemblyDir
Path for the web projects dll
.PARAMETER Versions
Api versions to generate the OpenApiDoc for and compare with baseline
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

foreach ($Version in $Versions)
{
    $old=(Join-Path -Path "$SwaggerDir" -ChildPath "$Version/swagger.yaml")
    $new="/$SwaggerDir/$Version.yaml"
    write-host "Running comparison with baseline for version $Version"
    Write-Host "old: $old"
    Write-Host "new: $new"
    docker run --rm -t -v "${pwd}/${SwaggerDir}:/swagger:ro" $container /$old $new --fail-on-incompatible
}
