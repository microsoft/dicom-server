# Dicom  
This document uses c# to demonstrate working with the Medical Imaging Server for DICOM.

For the tutorial we will use the BlueCircle.dcm, GreenSquare.dcm, and RedTriangle.dcm found in this repo.(TODO: Insert Link)
|File|StudyUID|SeriesUID|InstanceUID|
|---|---|---|---|---|
|GreenSquare.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652|1.2.826.0.1.3680043.8.498.12714725698140337137334606354172323212|
|RedTriangle.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652|1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395|
|BlueCircle.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.77033797676425927098669402985243398207|1.2.826.0.1.3680043.8.498.13273713909719068980354078852867170114|


## Create a DicomWeb Client
---
After you have started your Dicom Server, get the URL for the server and run the following code snippet to create DicomWeb Client which we will be using for the rest of the tutorial.

```c#
string webServerUrl ="{Your DicomWeb Server URL}"
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri(webServerUrl);
DicomWebClient client = new DicomWebClient(httpClient);
```
With the Dicom Web client we can now perform Store, Retrieve, Search, and Delete operations.
## Uploading DICOM (STOW)
---
Using the DicomwebClient that we have creatd we can now Store Dicom files to your Dicom Server.

### Store-single-instance
This demonstrates how to upload a single Dicom file to the server.
_Details:_
* Path: ../studies
* Method: POST
```c#
DicomFile dicomFile = DicomFile.Open(@"{Path To BlueCircle.dcm}");
DicomWebResponse response = await client.StoreAsync(new[] { dicomFile });
```
### Store-instances-for-a-specific-study

This  demonstrates how to upload a Dicom file into a specified study.

_Details:_
* Path: ../studies/{study}
* Method: POST

```c#
DicomFile dicomFile = DicomFile.Open(@"{Path To RedTriangle.dcm}");
DicomWebResponse response = await client.StoreAsync(new[] { dicomFile }, "1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420");
```

## Retrieving DICOM (WADO)
---
The following code snippets will demonstrate how to perform each of the retrieve queries using the DicomWeb Client we created.

### Retrieve-all-instances-within-a-study
This request retrieves all instances within a single study.

_Details:_
* Path: ../studies/{study}

```c#

```


