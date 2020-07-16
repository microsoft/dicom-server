$CurrentDirectory = ($pwd).path

$CommonModule = -join($CurrentDirectory, '\', 'Common.psm1')
Import-Module $CommonModule -Force

$txt = '.txt'

$ResourceGroup = Read-Host -Prompt 'Input resource group name'
$Namespace = Read-Host -Prompt 'Input Service Bus Namespace name'
$InstanceCount = Read-Host -Prompt 'Input total count of instances'
$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

$InstanceCountPerThread = $InstanceCount / $ConcurrentThreads
$PersonGeneratorProject = -join($CurrentDirectory, '\Microsoft.Health.Dicom.Tools.ScaleTesting.PersonInstanceGenerator')
$PersonGeneratorApp = -join ($PersonGeneratorProject, '\bin\Release\netcoreapp3.1\Microsoft.Health.Dicom.Tools.ScaleTesting.PersonInstanceGenerator')
$PatientNames = -join($CurrentDirectory, '\PatientNames.txt')
$PhysicianNames = -join($CurrentDirectory, '\PhysiciansNames.txt')

build($PersonGeneratorProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{
	$fileName = -join($CurrentDirectory, '\', $i, $txt)
	Start-Process -FilePath $PersonGeneratorApp -ArgumentList "$PatientNames, $PhysicianNames, $fileName, $InstanceCountPerThread"
}
    