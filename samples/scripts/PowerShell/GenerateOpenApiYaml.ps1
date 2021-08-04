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

    [string]$SwashbuckleCLIVersion = '6.1.4'
)

dotnet new tool-manifest --force
dotnet tool install --version $SwashbuckleCLIVersion Swashbuckle.AspNetCore.Cli

docker create -v ${SwaggerDir}:/swagger --name openAPIDiff openapitools/openapi-diff:latest@sha256:5da8291d3947414491e4c62de74f8fc1ee573a88461fb2fb09979ecb5ea5eb02

foreach ($Version in $Versions)
{
    write-host "Generating Yaml file for $Version"

    dotnet swagger tofile --yaml --output (Join-Path -Path "$SwaggerDir" -ChildPath "$Version.yaml") "$AssemblyDir" $Version

    write-host "Running comparison with baseline for version $Version"
    docker run --rm -t --volumes-from openAPIDiff openapitools/openapi-diff:latest@sha256:5da8291d3947414491e4c62de74f8fc1ee573a88461fb2fb09979ecb5ea5eb02 "/swagger/$Version/swagger.yaml" "/swagger/$version.yaml" --fail-on-incompatible

}

docker rm openAPIDiff --force
