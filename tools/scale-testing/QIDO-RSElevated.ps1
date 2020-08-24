$CurrentDirectory = (pwd).path

$CommonModule = -join($CurrentDirectory, '\', 'Common.psm1')
Import-Module $CommonModule -Force

$txt = '.txt'
$topicName = 'qido'

$ResourceGroup = Read-Host -Prompt 'Input resource group name'
$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

$Namespace = Read-Host -Prompt 'Input Service Bus Namespace name'
$AppName = Read-Host -Prompt 'Input App Service Name'

build($QueryGeneratorProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{
	$fileName = -join($CurrentDirectory, '\', $i, $txt)
	$outputFileName = -join($CurrentDirectory, '\', $i, 'queries', $txt)
	Start-Process -FilePath $QueryGeneratorApp -ArgumentList "$fileName $outputFileName" -RedirectStandardError "log.txt"
}

Read-Host -Prompt 'Press any key to continue once the QueryGenerator processes are completed.'

build($MessageUploaderProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{    
	$fileName = -join($CurrentDirectory, '\', $i, 'queries', $txt)
    $TotalCount = Get-Content $fileName | Measure-Object –Line
    $Lines = $TotalCount.Lines
	Start-Process -FilePath $MessageUploaderApp -ArgumentList "$topicName $fileName $zero $Lines" -RedirectStandardError "log.txt"
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