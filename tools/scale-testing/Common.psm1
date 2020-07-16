 Import-Module Az.Websites -Force
 
 # Set the Azure Subscription in current PowerShell context.
function setAzureSubscription($basePath){
    $path = '{0}/{1}' -f $basePath, 'rami.publishsettings'
    Import-AzurePublishSettingsFile -PublishSettingsFile $path | Out-Null
    Select-AzureSubscription -SubscriptionName "Visual Studio Ultimate with MSDN" | Out-Null
    Write-Host "$(Get-Date –f $timeStampFormat) - Set Azure subscription Completed " -foregroundcolor "green"
}

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
    [System.IO.Compression.ZipFile]::CreateFromDirectory($path, $zipPath,
                                                          $compressionLevel, 
                                                          $false) 
    Write-Host "$(Get-Date –f $timeStampFormat) - Zip Package Created " -foregroundcolor "green"
}

# Deploy the ZIP Package to AzureWebsite using New-AzureWebsiteJob cmdlet.
function deploy([String]$resourceGroupName, [String]$appName, [String]$basepath){
    $zipPath = '{0}/{1}' -f $basepath, 'bin\Release.zip'
    #Select-AzureSubscription -Default -SubscriptionName "Resolute SandBox" | Out-Null
    #Write-Host "$(Get-Date –f $timeStampFormat) - Set Azure subscription Completed " -foregroundcolor "green"
    $DebugPreference= "Continue"
    Connect-AzAccount
    Publish-AzWebApp -ArchivePath $zipPath -ResourceGroupName $resourceGroupName -Name $appName
    Write-Host "$(Get-Date –f $timeStampFormat) - Completed Deployment " -foregroundcolor "green"
}