# Export DICOM Files

The Medical Imaging Server for DICOM supports the bulk export of data to an [Azure Blob Storage account](https://azure.microsoft.com/en-us/services/storage/blobs/). Before starting, be sure the feature is enabled in your configuration by setting `DicomServer:Features:EnableExtendedExport` to `true`.

The export API is available at `POST /export`. Given a *source*, the set of data to be exported, and a *destination*, the location to which data will be exported, the endpoint returns a reference the newly-started long-running export operation. The duration of this operation depends on the volume of data to be exported.

## Request

The request body consists of the export source and destination.

```json
{
    "source": {
        "type": "<source type>",
        "settings": {
            "setting1": "<value>",
            "setting2": "<value>"
        }
    },
    "destination": {
        "type": "<destination type>",
        "settings": {
            "setting3": "<value>"
        }
    }
}
```

## Response

Upon successfully starting an export operation, the export API returns a `202` status code. The body of the response contains a reference to the operation, while the value of the `Location` header is the URL for the export operation's status (the same as `href` in the body).

```json
{
    "id": "df1ff476b83a4a3eaf11b1eac2e5ac56",
    "href": "<base url>/<version>/operations/df1ff476b83a4a3eaf11b1eac2e5ac56"
}
```

## Errors

If there are any errors when exporting a DICOM file (that was determined not to be a problem with the client), then the file is skipped and its corresponding error is logged. This error log is also exported alongside the DICOM files and can be reviewed by the caller. Each destination will include an error log.

### Format

Each line of the error log is a JSON object with the following properties. Note that a given error identifier may appear multiple times in the log as each update to the log is processed *at least once*.

| Property     | Description |
| ------------ | ----------- |
| `Timestamp`  | The date and time when the error occurred |
| `Identifier` | The identifier for the DICOM study, series, or SOP instance in the format of `"<study instance UID>[/<series instance UID>[/<SOP instance UID>]]"` |
| `Error`      | The error message |

## Sources

While the API can support different sources, only `"identifiers"` are supported today.

### Identifiers

**Type**: `"identifiers"`

The identifiers source can be used to specify a list of DICOM studies, series, and/or SOP instances for export.

#### Settings

The only setting is the list of identifiers to export.

| Property | Required | Default | Description |
| :------- | :------- | :------ | :---------- |
| `Values` | Yes      |         | A list of one or more DICOM studies, series, and/or SOP instances identifiers in the format of `"<study instance UID>[/<series instance UID>[/<SOP instance UID>]]"` |

#### Example

```json
{
    "type": "identifiers",
    "settings": {
        "values": [
            "1.2.3",
            "12.3/4.5.678",
            "123.456/7.8/9.1011.12"
        ]
    }
}
```

## Destinations

While the API can support different destinations, only `"azureblob"` is supported today.

### Azure Blob

**Type**: `"azureblob"`

The Azure Blob storage destination can be used to export DICOM files to an existing Azure Blob container.

#### Settings

The connection to the Azure Blob storage account can be specified with either a `ConnectionString` and `BlobContainerName` or a `BlobContainerUri`. One of these settings is required!

If the storage account requires authentication, a [SAS token](https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview) can be included in either the `ConnectionString` or `BlobContaienrUri`. A managed identity can also be used to access the storage account with the `UseManagedIdentity` option. Note that the identity used by both the DICOM server and the functions must have access to the container.

| Property             | Required | Default | Description |
| :------------------- | :------- | :------ | :---------- |
| `BlobContainerName`  | No       | `""`    | The name of a blob container. Only required when `ConnectionString` is specified |
| `BlobContainerUri`   | No       | `""`    | The complete URI for the blob container                      |
| `ConnectionString`   | No       | `""`    | The [Azure Storage connection string](https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string) that must minimally include information for blob storage |
| `UseManagedIdentity` | No       | `false` | An optional flag indicating whether managed identity should be used to authenticate to the blob container |

#### Error Log

The error log can be found at `<export blob container uri>/<operation ID>/Errors.log`.

### Example

```json
{
    "type": "azureblob",
    "settings": {
        "blobContainerName": "export",
        "connectionString": "UseDevelopmentStorage=true"
    }
}
```
