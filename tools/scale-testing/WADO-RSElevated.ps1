$CurrentDirectory = (pwd).path

$CommonModule = -join($CurrentDirectory, '\', 'Common.psm1')
Import-Module $CommonModule -Force

$txt = '.txt'
$topicName = 'wado-rs'

$RunType = 'instances'
$InputRunType = Read-Host -Prompt 'Is this run for instances, series or studies? Default is instances.'
switch($InputRunType)
{
    'series'{   $RunType = 'series'    }
    'studies'{   $RunType = 'studies'    }
}

Write-Host "Run Type is $RunType"

$ResourceGroup = Read-Host -Prompt 'Input resource group name'
$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

$Namespace = Read-Host -Prompt 'Input Service Bus Namespace name'
$AppName = Read-Host -Prompt 'Input App Service Name'

$fileName = -join($CurrentDirectory, '\', $RunType, $txt)

$TotalCount = Get-Content $fileName | Measure-Object –Line

$UnroundedCountPerThread = $TotalCount.Lines / $ConcurrentThreads

$CountPerThread = [Math]::Floor([decimal]($UnroundedCountPerThread))

build($MessageUploaderProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{
    $start = $i * $CountPerThread
    $end = $start + $CountPerThread
	Start-Process -FilePath $MessageUploaderApp -ArgumentList "$topicName $fileName $start $end" -RedirectStandardError "log.txt"
}

$SubscriptionState = Get-AzServiceBusSubscription -ResourceGroup $ResourceGroup -NamespaceName $Namespace -TopicName $topicName -SubscriptionName 's1'
while($SubscriptionState.properties.messageCount -lt $InstanceCount)
{
    Start-Sleep -s 60
    $SubscriptionState = Get-AzServiceBusSubscription -ResourceGroup $ResourceGroup -NamespaceName $Namespace -TopicName $topicName -SubscriptionName 's1'
}

Start-Sleep -s 120

build($MessageHandlerProject)
createPackage($MessageHandlerProject)
deploy -resourceGroupName $ResourceGroupName -appName $AppName -basepath $MessageHandlerProject