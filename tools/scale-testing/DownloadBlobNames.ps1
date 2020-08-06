$CurrentDirectory = ($pwd).path

$CommonModule = -join($CurrentDirectory, '\', 'Common.psm1')
Import-Module $CommonModule -Force

$txt = '.txt'
$RetrieveBlobNamesProject = -join($CurrentDirectory, '\Microsoft.Health.Dicom.Tools.ScaleTesting.RetrieveBlobNames')
$RetrieveBlobNamesApp = -join ($RetrieveBlobNamesProject, '\bin\Release\netcoreapp3.1\Microsoft.Health.Dicom.Tools.ScaleTesting.RetrieveBlobNames.exe')

build($RetrieveBlobNamesProject)

$instancesFileName = -join($CurrentDirectory, 'instances', $txt)
$seriesFileName = -join($CurrentDirectory, 'series', $txt)
$studiesFileName = -join($CurrentDirectory, 'studies', $txt)
Start-Process -FilePath $RetrieveBlobNamesApp -ArgumentList "$instancesFileName $seriesFileName $studiesFileName"