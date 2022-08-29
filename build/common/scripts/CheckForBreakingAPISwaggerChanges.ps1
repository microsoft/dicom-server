<#
.SYNOPSIS
Generates the OpenApi doc for the specified version and compares it with the baseline to make sure no breaking changes are introduced
Run script from root of this repository
.Parameter SwaggerDir
Swagger directory path from root of this repository. Ex: 'swagger'
.PARAMETER Versions
Api versions to generate the OpenApiDoc for and compare with baseline
#>

param(
    [string]$SwaggerDir,

    [String[]]$Versions
)

$ErrorActionPreference = 'Stop'
$container="openapitools/openapi-diff:latest@sha256:442d61387d4d3c5bff282f1eb30decffa87c8c4acae77e6ac3f815b1f63672ea"

if (Test-Path "$SwaggerDir/FromMain") { Remove-Item -Recurse -Force "$SwaggerDir/FromMain" }
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
    docker run --rm -t -v "${pwd}/${SwaggerDir}:/${SwaggerDir}:ro" $container /$old /$new --fail-on-incompatible
}

Remove-Item -Recurse -Force "$SwaggerDir/FromMain"
