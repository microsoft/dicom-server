$CurrentDirectory = ($pwd).path

$CommonModule = -join($CurrentDirectory, '\', 'Common.psm1')
Import-Module $CommonModule -Force

$txt = '.txt'
$topicName = 'qido'

$ResourceGroup = Read-Host -Prompt 'Input resource group name'
$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

$QueryGeneratorProject = -join($CurrentDirectory, '\Microsoft.Health.Dicom.Tools.ScaleTesting.QidoQueryGenerator')
$QueryGeneratorApp = -join ($QueryGeneratorProject, '\bin\Release\netcoreapp3.1\Microsoft.Health.Dicom.Tools.ScaleTesting.QidoQueryGenerator.exe')

build($QueryGeneratorProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{
	$fileName = -join($CurrentDirectory, '\', $i, $txt)
	$outputFileName = -join($CurrentDirectory, '\', $i, 'queries', $txt)
	Start-Process -FilePath $QueryGeneratorApp -ArgumentList "$fileName $outputFileName" -RedirectStandardError "log.txt"
}

$null = Read-Host 'Press any key to continue'

$MessageUploaderProject = -join($CurrentDirectory, '\Microsoft.Health.Dicom.Tools.ScaleTesting.MessageUploader')
$MessageUploaderApp = -join ($MessageUploaderProject, '\bin\Release\netcoreapp3.1\Microsoft.Health.Dicom.Tools.ScaleTesting.MessageUploader.exe')

build($MessageUploaderProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{    
	$fileName = -join($CurrentDirectory, '\', $i, $txt)
    $TotalCount = Get-Content $fileName | Measure-Object –Line
	Start-Process -FilePath $MessageUploaderApp -ArgumentList "$topicName $fileName 0 $TotalCount" -RedirectStandardError "log.txt"
}