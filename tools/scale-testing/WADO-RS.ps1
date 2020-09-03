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

$ConcurrentThreads = Read-Host -Prompt 'Input threads to run simultaneously for upload'

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
