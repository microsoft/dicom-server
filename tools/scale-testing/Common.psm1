 Import-Module Az.Websites -Force
 $global:CurrentDirectory = (pwd).path
 $global:zero  = '0'

# Build the console application using DotNet CLI.
function build($basePath){
    $path = '{0}/{1}' -f $basePath, 'bin'
    Get-ChildItem -Path $path -Include *.* -File -Recurse | foreach { $_.Delete()}  
    Write-Host "$(Get-Date –f $timeStampFormat) - Bin Directory Completed " -foregroundcolor "green"

    dotnet build --configuration release $basePath | Out-Null
    Write-Host "$(Get-Date –f $timeStampFormat) - Dotnet Build Completed " -foregroundcolor "green"
}

# Create the ZIP package using System.IO API.
function createPackage($basePath){    
    $path = '{0}/{1}' -f $basePath, 'bin\Release'
    $zipPath = '{0}/{1}' -f $basepath, 'bin\Release.zip'

    Add-Type -Assembly System.IO.Compression.FileSystem 
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($path,
                                                          $zipPath,
                                                          $compressionLevel, 
                                                          $false) 
    Write-Host "$(Get-Date –f $timeStampFormat) - Zip Package Created " -foregroundcolor "green"
}

# Deploy the ZIP Package to AzureWebsite using New-AzureWebsiteJob cmdlet.
function deploy([String]$resourceGroupName, [String]$appName, [String]$basepath){
    $zipPath = '{0}/{1}' -f $basepath, 'bin\Release.zip'
    $DebugPreference= "Continue"
    Connect-AzAccount
    Publish-AzWebApp -ArchivePath $zipPath -ResourceGroupName $resourceGroupName -Name $appName
    Write-Host "$(Get-Date –f $timeStampFormat) - Completed Deployment " -foregroundcolor "green"
}

function generateProject([String] $ProjectName){
    -join($CurrentDirectory, "\", $ProjectName)
}

function generateApplicationFromProject([String]$Project, [String]$ProjectDirectory){
    -join($ProjectDirectory, "\bin\Release\netcoreapp3.1\", $Project, ".exe")
}

$global:PersonGenerator = 'PersonInstanceGenerator'
$global:PersonGeneratorProject = generateProject $PersonGenerator
$global:PersonGeneratorApp = generateApplicationFromProject $PersonGenerator $PersonGeneratorProject

$global:RetrieveBlobNames = 'RetrieveBlobNames'
$global:RetrieveBlobNamesProject = generateProject $RetrieveBlobNames
$global:RetrieveBlobNamesApp = generateApplicationFromProject $RetrieveBlobNames $RetrieveBlobNamesProject

$global:MessageUploader = 'MessageUploader'
$global:MessageUploaderProject = generateProject $MessageUploader
$global:MessageUploaderApp = generateApplicationFromProject $MessageUploader $MessageUploaderProject

$global:QidoQueryGenerator = 'QidoQueryGenerator'
$global:QueryGeneratorProject = generateProject $QidoQueryGenerator
$global:QueryGeneratorApp = generateApplicationFromProject $QidoQueryGenerator $QueryGeneratorProject

$global:MessageHandlerProject = generateProject('MessageHandler')