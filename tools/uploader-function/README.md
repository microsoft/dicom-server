# Uploader Function

This solution uses the [Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/) framework to allow uploads to a DICOM Service. The instructions below utilize the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/) for setting up the environment

Steps:
## 1. Create app service plan
This step creates a Linux premium app service plan that is able to host a container function.

```
az functionapp plan create --resource-group {resource-group-name} --name {app-service-plan-name} --location {REGION} --number-of-workers 1 --sku EP1 --is-linux
```

| Property | Value |
| --- | --- |
| {resource-group-name} | The resource group that you would like to place your app service plan in. |
| {app-service-plan-name} | The name of your app service plan.

## 2. Create function app
This step creates your function app. 

> Note: It is safe to ignore warnings about the lack of credentials for accessing the container registry as it is publically accessible.

```
az functionapp create --name {function-app-name} --storage-account {storage-account-name} --resource-group {resource-group-name} --plan {app-service-plan-name} --deployment-container-image-name dicomoss.azurecr.io/dicom-uploader:0.0.1 --functions-version 4 --assign-identity [system]
```

| Property | Value |
| --- | --- |
| {function-app-name} | The name of your uploader function app. |
| {storage-account-name} | The name of the storage account that the function will keep state in. Can be the same or different from the the DICOM file source. |
| {resource-group-name} | The resource group you would like to place your function app in. | 
| {app-service-plan-name} | The app service plan you created in the previous step. |

## 3. Grant permissions for function's managed identity

The managed identity for the function needs the following permissions to execute.

| Role | Resource |
| --- | --- |
| Dicom Data Owner | The DICOM Service that the uploader will write to. |
| [Storage Blob Data Contributor](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor) | The storage account used as athe source of the DICOM files. |
| [Storage Queue Data Contributor](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-queue-data-contributor) | The storage account used as athe source of the DICOM files. |


## 4. Update settings
This step updates the configuration for your uploader function app to run. 

```
az functionapp config appsettings set --name {function-app-name} --resource-group {resource-group-name} --settings "sourceblobcontainer={source-container-name}" "sourcestorage__blobServiceUri={source-blob-url}" "sourcestorage__queueServiceUri={source-queue-url}" "DicomWeb__Endpoint={dicom-service-endpoint}" "DicomWeb__Authentication__Enabled=true" "DicomWeb__Authentication__AuthenticationType=ManagedIdentity" "DicomWeb__Authentication__ManagedIdentityCredential__Resource=https://dicom.healthcareapis.azure.com"
```

| Property | Value |
| --- | --- |
| {function-app-name} | The name of your uploader function app. |
| {resource-group-name} | The resource group of your function app. | 
| {source-container-name} | The name of the container that contains your DICOM files located in the storage service referenced by {source-blob-url}. |
| {source-blob-url} | The URL of your blob service that contains your DICOM files. |
| {source-queue-url} | The URL of your queue service of the storage account that contains your DICOM files. This service is used to create a poison queue and messages in if there is a failure to upload a DICOM file. |
| {dicom-service-endpoint} | The DICOM Service endpoint that you wish to upload DICOM files to. |

## Troubleshooting

* There is an application insights published along with the function that will contain traces and exceptions from the running of the service. You can find more about how to [Query telemetry data](https://docs.microsoft.com/en-us/azure/azure-functions/analyze-telemetry-data#query-telemetry-data)
* When a blob is processed by the function it writes a [blob receipt](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-trigger?tabs=in-process%2Cextensionv5&pivots=programming-language-csharp#blob-receipts) into the {storage-account-name} specified in step 1.
* When a blob fails to be processed it writes a [Poison blob](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-trigger?tabs=in-process%2Cextensionv5&pivots=programming-language-csharp#poison-blobs) into the queue service specified in {source-queue-url} above.