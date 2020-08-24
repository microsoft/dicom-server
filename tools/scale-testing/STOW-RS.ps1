$CurrentDirectory = (pwd).path
$CommonModule = -join($CurrentDirectory, '\', 'Common.psm1')
Import-Module $CommonModule -Force

$txt = '.txt'

$InstanceCount = Read-Host -Prompt 'Input total count of instances'
$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

$UnroundedInstanceCountPerThread = $InstanceCount / $ConcurrentThreads
$InstanceCountPerThread = [Math]::Floor([decimal]($UnroundedInstanceCountPerThread))
$PatientNames = -join($CurrentDirectory, '\PatientNames.txt')
$PhysicianNames = -join($CurrentDirectory, '\PhysiciansNames.txt')

build($PersonGeneratorProject)
for($i = 0; $i -lt $ConcurrentThreads; $i++)
{
	$fileName = -join($CurrentDirectory, '\', $i, $txt)
	Start-Process -FilePath $PersonGeneratorApp -ArgumentList "$PatientNames $PhysicianNames $fileName $InstanceCountPerThread" -RedirectStandardError "log.txt"
}
    