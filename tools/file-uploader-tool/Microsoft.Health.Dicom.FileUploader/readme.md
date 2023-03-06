# Overview
This command-line tool can be used to upload DICOM file(s) to a target DICOMWeb endpoint. The default behavior is to upload the file(s) in the [Images](./Images) folder, or you
can specify a directory to upload all files with the `.dcm` file extension.

This tool can be run locally, or from an Azure VM with a managed identity enabled.

# Arguments
## --dicomServiceUrl
Dicom service URL to target. If not specified, will default to the standard port for the DICOM server running locally: `https://localhost:63838`.

## --path
Path to a directory containing `.dcm` files to be uploaded. If not specified, will default to `/Images`.

## --deleteFiles
If specified, will delete successfully uploaded files.

# Example
To upload the sample file, run:
```
dotnet run --dicomServiceUrl "https://testdicomweb-testdicom.dicom.azurehealthcareapis.com"
```

To upload all `.dcm` files in a specific directory to a locally running service, run:
```
dotnet run --path C:\dicomdir
```
