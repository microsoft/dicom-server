$CurrentDirectory = ($pwd).path

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

"Run Type is $RunType"

$ResourceGroup = Read-Host -Prompt 'Input resource group name'
$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

$fileName = -join($CurrentDirectory, '\', $RunType, $txt)

$TotalCount = Get-Content $fileName | Measure-Object –Line

$CountPerThread = $TotalCount / $ConcurrentThreads
$MessageUploaderProject = -join($CurrentDirectory, '\Microsoft.Health.Dicom.Tools.ScaleTesting.MessageUploader')
$MessageUploaderApp = -join ($MessageUploaderProject, '\bin\Release\netcoreapp3.1\Microsoft.Health.Dicom.Tools.ScaleTesting.MessageUploader.exe')

build($MessageUploaderProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{
    $start = $i * $CountPerThread
    $end = $start + $CountPerThread
	Start-Process -FilePath $MessageUploaderApp -ArgumentList "$topicName $fileName $start $end" -RedirectStandardError "log.txt"
}
