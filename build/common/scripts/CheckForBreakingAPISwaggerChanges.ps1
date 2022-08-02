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

mkdir "$SwaggerDir/FromMain"

foreach ($Version in $Versions)
{
    $new=(Join-Path -Path "$SwaggerDir" -ChildPath "$Version/swagger.yaml")
    $old=(Join-Path -Path "$SwaggerDir" -ChildPath "/FromMain/$Version.yaml")

    $SwaggerOnMain="https://raw.githubusercontent.com/microsoft/dicom-server/main/swagger/$Version/swagger.yaml"
    Invoke-WebRequest -Uri $SwaggerOnMain -OutFile $old


    write-host "Running comparison with baseline for version $Version"
    Write-Host "old: $old"
    Write-Host "new: $new"
    docker run --rm -t -v "${pwd}/${SwaggerDir}:/swagger:ro" $container /$old /$new --fail-on-incompatible
    rm $old
}

rmdir "$SwaggerDir/FromMain"
