# takes a folder of fhir records and extracts patient ids 
# sets patient ids on dicom records and renames them to associated fhir bundle


#region processFhirBundle
# look in a folder named 'FHIR Data' and examine all json files
# Assumes 1 patient per bundle

$files = dir '.\FHIR Data\' -Recurse *.json
$records = @()
foreach($file in $files){
    # $file = $files[0]
    $content = get-content  -Raw $file.fullname
    $name = $file.directory.name + "_" + $file.BaseName
    $object = ConvertFrom-Json -InputObject $content -AsHashtable -Depth 45
    foreach($entry in $object.entry){
        if($entry.resource.resourceType -eq "Patient"){
            $id = $entry.resource.id
            # write-host "@{`"name`"=`"$name`";`"id`"=`"$id`"},"
            $records += @(@{"name"=$name; "id"=$id})
        }
    }
}
#endregion


#region processDicomFiles

$env:Path+= ";.\bin;"

# dicom files in matched_dicoms
# it also renames them
# bin folder came from https://dicom.offis.de/download/dcmtk/dcmtk366/bin/dcmtk-3.6.6-win64-dynamic.zip

$files = dir '.\matched_dicoms\' -Recurse *.dcm | %{$_.fullname}

$file = $files[0]
dcmdump.exe $file

$indicies = 0..($records.count()-1)
foreach($i in $indicies){
    $id = $records[$i].id
    $file = $files[$i]
    dcmodify -i "(0010,0020)=$id" $file
    Rename-Item $file "$($records[$i].name + '.dcm')"
}
#endregion