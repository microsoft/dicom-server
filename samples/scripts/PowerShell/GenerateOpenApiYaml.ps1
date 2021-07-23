<#
.SYNOPSIS 
Generates the OpenApi doc for the specified version and compares it with the baseline to make sure no breaking changes are introduced
.Parameter WorkingDir
The working directory
.PARAMETER OutputPathofDll
Path for the web projects dll
.PARAMETER OutputPathOfOpenApiDoc
Path to output the OpenApi Doc (in yaml)
.PARAMETER Version
Api version to generate the OpenApiDoc for
#>

param(
    [string]$WorkingDir,

    [string]$OutputPathofDll,

    [string]$OutputPathOfOpenApiDoc,

    [string]$Version
)
write-host "Generating Yaml file for $Version"

dotnet new tool-manifest --force
dotnet tool install --version 6.1.4 Swashbuckle.AspNetCore.Cli
dotnet swagger tofile --yaml --output $WorkingDir/$OutputPathOfOpenApiDoc/$Version.yaml $WorkingDir/$OutputPathofDll $Version

write-host "Running comparison with baseline"
docker run --rm -t -v ${WorkingDir}:/dicom-server openapitools/openapi-diff:latest /dicom-server/swagger/$Version/swagger.yaml /dicom-server/$OutputPathOfOpenApiDoc/$version.yaml --fail-on-incompatible
