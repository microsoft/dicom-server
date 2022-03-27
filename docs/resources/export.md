# Exporting DICOM data 

This document explains how to export DICOM instances to a external source like `Azure blob storage`.

If you have many DICOM instances, too many to programatically call WADO-RS APIs for each, export provides an easy way to buck moves all the instances in a secure, resilient, performant way.

## Export Pre-requirement

- Create a external storage account
- Permissions setup


## Export API example

```cmd
POST /export
{
    source: {
        idFilter: {
            ids: [
                "studyUid",
                "studyUid/SeriesUid",
                "studyUid/SeriesUid/InstanceUid"
              ]
        }
    }
    destination: {
        azureStorage: {
            uri: "https://foobar.blob.core.windows.net/exportcontainer"
        }
    }
}
```
