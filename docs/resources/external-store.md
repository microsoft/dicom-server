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

#### Container Does Not Exist
The container specified must exist.

#### ServiceStorePath invalid
[See rules applied when blobs in accounts have a hierarchical namespace](https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names).
A summary:
```text
Reserved URL characters must be properly escaped.
...
If your account does not have a hierarchical namespace, then the number of path segments comprising the blob name 
cannot exceed 254.
...
By default, the Blob service is based on a flat storage scheme, not a hierarchical scheme. However, you may specify 
a character or string delimiter within a blob name to create a virtual hierarchy.
```
##### ExternalDataStoreInvalidCharactersInServiceStorePath
DICOM allows any alphanumeric characters, dashes(-). periods(.) and forward slashes (/) in the service store path.

For the rule stating that [`reserved URL characters must be properly escaped`](https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names), 
see [rfc section 2.2](https://www.rfc-editor.org/rfc/rfc3986#section-2.2):
```text
reserved    = gen-delims / sub-delims
gen-delims  = ":" / "/" / "?" / "#" / "[" / "]" / "@"
sub-delims  = "!" / "$" / "&" / "'" / "(" / ")" / "*" / "+" / "," / ";" / "="
```    

It is not enough to just encode all of these characters. The blob file store will not recognize an encoded forward 
slash as a delimiter as a logical directory segment. Because of this, DICOM has restricted allowable characters at 
this time.

When DICOM is a managed service, your workspace and DICOM names will be used to generate the path and this error 
should not occur:
- The workspace name can contain only lowercase letters, and numbers. No "-" or other symbols are allowed.
- The DICOM name can contain only lowercase letters, numbers and the '-' character, and must start and end with a 
  letter or a number.

##### ExternalDataStoreInvalidServiceStorePathSegments

[`If your account does not have a hierarchical namespace, then the number of path segments comprising the blob name
cannot exceed 254.`](https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names)

Modify the service store path to have less than 254 segments ad denoted by forward slashes (/).

##### ExternalDataStoreBlobNameTooLong

[`A blob name must be at least one character long and cannot be more than 1,024 characters long, for blobs in Azure Storage.`](https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names)

In the case of a flat storage scheme where virtual hierarchy is present, the blob name is the full path to the blob, 
including the directories.

You must either shorten the service store path or shorten the blob name overall.