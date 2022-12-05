# Blob migration

Currently DICOM files are stored with DICOM UIDs as blob names in blob storage, using the template `{account}/{container}/{studyUid}/{seriesUid}/{sopInstanceUid}_{watermark}.dcm`. 
Since UIDs may include personal information about the context of their creation, such as patient information or identifiers, we made the decision to change the way that we store DICOM files. In the next sections we list the steps to migrate your existing blobs from the old format to the new format.

## Blob migration configuration
Below is the `appsettings.json` configuration related to blob migration. Several properties need to be updated to trigger migration.

```json
"DicomServer": {
    "Services": {
      "BlobMigration": {
        "FormatType": "Old",
        "StartCopy": false,
        "StartDelete": false,
        "CopyFileOperationId": "1d4689da-ca3b-4659-b0c7-7bf6c9ff25e1",
        "DeleteFileOperationId": "ce38a27e-b194-4645-b47a-fe91c38c330f",
        "CleanupDeletedFileOperationId": "d32a0469-9c27-4df3-a1e8-12f7f8fecbc8",
        "CleanupFilterTimeStamp": "2022-08-01"
      }
    }
}
```

## Migration steps

1. If you are a new service and have not created any files, you can upgrade to the latest version of the service and skip the migration steps. That means you will see the BlobMigration.FormatType as "New" in appsettings configuration file. 

2. If you have already uploaded DICOM files but does not care about previous data. You can use the [Delete API](../resources/conformance-statement.md#delete) to delete all the data which can free up space and then upgrade to the latest version of the service or you can upgrade to the latest version of the service and then use the [Delete API](../resources/conformance-statement.md#delete) to delete all the data.

3. If you have already uploaded DICOM files and want to keep the previous data, you need to execute the below steps to copy the files first. This scenario has two options depending on whether if you want interruption to the service or not. Make sure Azure monitor is configured to monitor the service before starting the migration, for more info on how to configure Azure monitor, please refer to [Azure Monitor](../how-to-guides/configure-dicom-server-settings.md#azure-monitor).

    3.1. If you are ok with interruption to the service, you can follow the steps below:

        3.1.1. Change the BlobMigration.StartCopy to "True" and restart the service. This will start the copy background service which will trigger the CopyFiles Azure Durable Function which will copy the old format DICOM files to new format.

        3.1.2. To ensure Copy has been completed, you can check Azure Monitor logs for `"Completed copying files."` message. This will indicate that the copy has been completed.

        3.1.3. Once the copy is completed, you can change the BlobMigration.FormatType to "New" and BlobMigration.StartDelete to "True" and restart the service. This will trigger Delete background service which will delete all the old format blobs only if the corresponding new format blobs exist and set the format to New. This is a safe operation and doesn't delete any blobs without checking for the existence of new format blobs.

        3.1.4. To ensure Delete has been completed, you can check Azure Monitor logs for `"Completed deleting files."` message. This will indicate that the delete has been completed.

    3.2. If you are not ok with interruption to the service, you can follow the steps below:

        3.2.1. Change the BlobMigration.FormatType to "Dual" and restart the service. This will duplicate any new DICOM files uploaded to both old and new format. 
        
        3.2.2 Follow steps 3.1.1 to 3.1.4 to complete the copy and delete operation.


**Note: If in case there is any issues or questions during migration, you can post your questions or issues in the [DICOM Server GitHub Discussions](https://github.com/microsoft/dicom-server/discussions/1561) page.**`

