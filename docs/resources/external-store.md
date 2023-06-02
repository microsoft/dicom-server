# External Store (Preview Feature)

NOTE: Preview features are not complete and are not supported for production use. These features are subject to change without notice.

It is possible to configure DICOM to use an external store to store and retrieve DICOM files. This is useful when 
you already have a storage account created and want to use it with DICOM instead of creating a new storage account.

## Configuration

You can configure DICOM to use an external store by enabling the feature in the appsettings file. Additionally,
provide the connection string to the storage account with a container name or provide a BlobContainerUri which also 
specifies a container name to use within it.

Ex:
```json
{
  "DicomServer": {
    "Features": {
      "EnableExternalStore": true
    }
  },
  "ExternalBlobStore": {
    "ServiceStorePath": "dicom/",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net",
    "ContainerName": "dicomservice",
    "UseManagedIdentity": false
  }
}
```

DICOM server will run independently of the external store being configured correctly. In the event that the store is 
not configured correctly, DICOM will log errors and continue to run. DICOM will not be able to store or retrieve.

### Possible Configuration Issues

It is possible that the external store is not configured correctly. In this case, DICOM will log errors and continue to run.
Issues may be:
- connection string is incorrect or the server does not have access to it
- container name is incorrect, does not exist or the server does not have access to it