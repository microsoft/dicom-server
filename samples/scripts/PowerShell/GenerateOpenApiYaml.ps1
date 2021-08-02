<#
.SYNOPSIS 
Generates the OpenApi doc for the specified version and compares it with the baseline to make sure no breaking changes are introduced
.Parameter WorkingDir
The working directory
.PARAMETER AssemblyDir
Path for the web projects dll
.PARAMETER Versions
Api versions to generate the OpenApiDoc for and compare with baseline
.PARAMETER SwashbuckleCLIVersion
Version of SwashbuckleCLI to use
#>

param(
    [string]$WorkingDir,

    [string]$AssemblyDir,

    [String[]]$Versions,

    [string]$SwashbuckleCLIVersion = '6.1.4'
)
foreach ($Version in $Versions)
{
    write-host "Generating Yaml file for $Version"

    dotnet new tool-manifest --force
    dotnet tool install --version $SwashbuckleCLIVersion Swashbuckle.AspNetCore.Cli
    dotnet swagger tofile --yaml --output "$WorkingDir/src/Microsoft.Health.Dicom.Web/bin/$Version.yaml" "$WorkingDir/$AssemblyDir" $Version

    write-host "Running comparison with baseline for version $Version"
    docker run --rm -t -v ${WorkingDir}:/dicom-server openapitools/openapi-diff:latest "/dicom-server/swagger/$Version/swagger.yaml" "/dicom-server/src/Microsoft.Health.Dicom.Web/bin/$version.yaml" --fail-on-incompatible

}
