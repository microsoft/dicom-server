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

ls "$SwaggerDir"
