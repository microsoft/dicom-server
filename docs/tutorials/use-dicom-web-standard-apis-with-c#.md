# Dicom  
This document uses c# to demonstrate working with the Medical Imaging Server for DICOM.

For the tutorial we will use the BlueCircle.dcm, GreenSquare.dcm, and RedTriangle.dcm found [here](../dcms)

| File | StudyUID | SeriesUID | InstanceUID |
| --- | --- | --- | ---|
|GreenSquare.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652|1.2.826.0.1.3680043.8.498.12714725698140337137334606354172323212|
|RedTriangle.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652|1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395|
|BlueCircle.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.77033797676425927098669402985243398207|1.2.826.0.1.3680043.8.498.13273713909719068980354078852867170114|

>Note that all three of these files are part of the same study and GreenSquare and RedTriangle are part of the same series.

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
```c#
DicomFile dicomFile = DicomFile.Open(@"{Path To BlueCircle.dcm}");
DicomWebResponse response = await client.StoreAsync(new[] { dicomFile });
```
---
### Store-instances-for-a-specific-study

This  demonstrates how to upload a Dicom file into a specified study.

_Details:_
* Path: ../studies/{study}

```c#
DicomFile dicomFile = DicomFile.Open(@"{Path To RedTriangle.dcm}");
DicomWebResponse response = await client.StoreAsync(new[] { dicomFile }, "1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420");
```

Before moving on to the next part also upload the GreenSquare.dcm file to the server using either of the methods above.

## Retrieving DICOM (WADO)
---
The following code snippets will demonstrate how to perform each of the retrieve queries using the DicomWeb Client we created.

The following variable will be used throghout the rest of the examples:
```c#
string studyInstanceUid = "1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420"; //StudyUID for All 3 examples
string seriesInstanceUid = "1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652"; //SeriesUID for GreenSquare and RedTriangle
string sopInstanceUId = "1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395"; //SopInstanceUID for RedTriangle
```

### Retrieve-all-instances-within-a-study
This retrieves all instances within a single study.

_Details:_
* Path: ../studies/{study}

```c#
DicomWebResponse response = await client.RetrieveStudyAsync(studyInstanceUid);
```
All three of the dcm files that we uploaded previously are part of the same study so the response should have value 3 (Todo: how to validate this by user and response code). 

---
### Retrieve-metadata-of-all-instances-in-study

This request retrieves the metadata for all instances within a single study.

_Details:_
* Path: ../studies/{study}/metadata
```c#
DicomWebResponse response = await client.RetrieveStudyMetadataAsync(studyInstanceUid); 
```
Since all three files are part of the same study the response should contain the metadata for all three files. (Todo: how to validate this by user and response code). 

---
### Retrieve-all-instances-within-a-series

This request retrieves all instances within a single series.

_Details:_
* Path: ../studies/{study}/series{series}
```c#
DicomWebResponse response = await client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);
```
This series has 2 instances (GreenSquare and RedTriangle), so this should return an OK status code and also both instances.
(Todo: How to validate both)

### Retrieve-metadata-of-all-instances-within-a-series

This request retrieves the metadata for all instances within a single study.

_Details:_
* Path: ../studies/{study}/metadata
```c#
DicomWebResponse response = await client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);
```
This series has 2 instances (GreenSquare and RedTriangle), so this should return an OK status code and also the metatdata for both instances.
(Todo: How to validate both)

---
### Retrieve-a-single-instance-within-a-series-of-a-study

This request retrieves a single instances, and returns it.

_Details:_
* Path: ../studies/{study}/series{series}/instances/{instance}
```c#
DicomWebResponse response = await client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUId);
```
This should only return the instance RedTriangle, validate that only one instance is returned and status code is OK.
(Todo: How to validate both)

---
### Retrieve-metadata-of-a-single-instance-within-a-series-of-a-study

This request retrieves the metadata for a single instances within a single study and series.

_Details:_
* Path: ../studies/{study}/series/{series}/instances/{instance}/metadata
```c#
DicomWebResponse response = await client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUId);
```
This should only return the metadata for the instance RedTriangle, validate that only the metadata for RedTriangle is returned and status code is OK.
(Todo: How to validate both)

---
### Retrieve-one-or-more-frames-from-a-single-instance

This request retrieves one or more frames from a single instance.

_Details:_
* Path: ../studies/{study}/series/{series}/instances/{instance}/frames/{frames}
```c#
DicomWebResponse response = await client.RetrieveFramesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUId,null, new[] { 1 });
```
This should return the only frame from the RedTriangle and status code should be OK.
(Todo:How to validate both)

## Query DICOM (QIDO)
---
### Search-for-studies

This request enables searches for one or more studies by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.
_Details:_
* Path: ../studies?StudyInstanceUID={{study}}
```c#
string query = "/studies?StudyInstanceUID=" + studyInstanceUid;
DicomWebResponse response = await client.QueryAsync(query);
```
Validate that response includes 1 study and that response code is OK.

---
### Search-for-series

This request enables searches for one or more series by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.
_Details:_
* Path: ../series?SeriesInstanceUID={{series}}
```c#
string query = "/series?SeriesInstanceUID=" + seriesInstanceUid;
DicomWebResponse response = await client.QueryAsync(query);
```
Validate that response includes 1 series and that response code is OK.

---
### Search-for-series-within-a-study

This request enables searches for one or more series within a single study by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.
_Details:_
* Path: ../studies/{{study}}/series?SeriesInstanceUID={{series}}
```c#
string query = "/studies/" + studyInstanceUid + "/series?SeriesInstanceUID=" + seriesInstanceUid;
DicomWebResponse response = await client.QueryAsync(query);
```
Validate that response includes 1 series and that response code is OK.

---
### Search-for-instances

This request enables searches for one or more instances by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.
_Details:_
* Path: ../instances?SOPInstanceUID={{instance}}
```c#
string query = "/instances?SOPInstanceUID=" + sopInstanceUId;
DicomWebResponse response = await client.QueryAsync(query);
```
Validate that response includes 1 instance and that response code is OK.

---
### Search-for-instances-within-a-study

This request enables searches for one or more instances within a single study by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.
_Details:_
* Path: ../studies/{{study}}/instances?SOPInstanceUID={{instance}}
```c#
string query = "/studies/" + studyInstanceUid + "/instances?SOPInstanceUID=" + sopInstanceUId;
DicomWebResponse response = await client.QueryAsync(query);
```
Validate that response includes 1 instance and that response code is OK.

---
### Search-for-instances-within-a-study-and-series

This request enables searches for one or more instances within a single study and single series by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.
_Details:_
* Path: ../studies/{{study}}/series/{{series}}instances?SOPInstanceUID={{instance}}
```c#
string query = "/studies/" +studyInstanceUid + "/series/" + seriesInstanceUid + "/instances?SOPInstanceUID=" + sopInstanceUId;
DicomWebResponse response = await client.QueryAsync(query);
```
Validate that response includes 1 instance and that response code is OK.


## Delete DICOM 
---
### Delete-a-specific-instance-within-a-study -and-series

This request deletes a single instance within a single study and single series.

> Delete is not part of the DICOM standard, but has been added for convenience.
_Details:_
* Path: ../studies/{{study}}/series/{{series}}/instances/{{instance}}
```c#
string sopInstanceUIdRed = "1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395";
DicomWebResponse response = await client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUIdRed);
```
This deletes the RedTriangle instance from the server. If it is successful the response status code contains no content.

---
### Delete-a-specific-series-within-a-study

This request deletes a single series (and all child instances) within a single study.

> Delete is not part of the DICOM standard, but has been added for convenience.
_Details:_
* Path: ../studies/{{study}}/series/{{series}}
```c#
DicomWebResponse response = await client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);
```
This deletes the GreenSquare instance (it is the only element left in the series) from the server. If it is successful the response status code contains no content.

---
### Delete-a-specific-study

This request deletes a single study (and all child series and instances).

> Delete is not part of the DICOM standard, but has been added for convenience.
_Details:_
* Path: ../studies/{{study}}
```c#
DicomWebResponse response = await client.DeleteStudyAsync(studyInstanceUid);
```
This deletes the BlueCircle instance (it is the only element left in the sries) from the server. If it is successful the response status code contains no content.
