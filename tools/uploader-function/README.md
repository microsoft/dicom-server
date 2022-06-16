# Uploader Function

This solution uses the Azure Function framework to allow uploads to a DICOM Service.

Steps:
## Create app service plan
```
az functionapp plan create --resource-group {resource-group-name} --name {app-service-plan-name} --location {REGION} --number-of-workers 1 --sku EP1 --is-linux
```

## Create function app
```
az functionapp create --name dicom-uploader --storage-account {storage-account-name} --resource-group {resource-group-name} --plan {app-service-plan-name} --deployment-container-image-name dicomoss.azurecr.io/dicom-uploader:0.0.1 --functions-version 4 --assign-identity [system]
```

## Grant permissions for function's managed identity

- Grant Function managed identity Dicom Data Owner
- Grant Function managed identity Storage Blob Contrib
- Grant Function Managed identity Storage Queeu Contrib

## Update settings
```
az functionapp config appsettings set --name dicom-uploader --resource-group {resource-group-name} --settings "sourceblobcontainer={source-container-name}" "sourcestorage__blobServiceUri={source-blob-url}" "sourcestorage__queueServiceUri={source-queue-url}" "DicomWeb__Endpoint={dicom-service-endpoint}" "DicomWeb__Authentication__Enabled=true" "DicomWeb__Authentication__AuthenticationType=ManagedIdentity" "DicomWeb__Authentication__ManagedIdentityCredential__Resource=https://dicom.healthcareapis.azure.com"
```
