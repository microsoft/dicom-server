# Use DICOMweb&trade; Standard APIs with C#

This tutorial uses C# to demonstrate working with the Medical Imaging Server for DICOM.

For the tutorial we will use the DICOM files here: [Sample DICOM files](../dcms). The file name, studyUID, seriesUID and instanceUID of the sample DICOM files is as follows:

| File | StudyUID | SeriesUID | InstanceUID |
| --- | --- | --- | ---|
|green-square.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652|1.2.826.0.1.3680043.8.498.12714725698140337137334606354172323212|
|red-triangle.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652|1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395|
|blue-circle.dcm|1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420|1.2.826.0.1.3680043.8.498.77033797676425927098669402985243398207|1.2.826.0.1.3680043.8.498.13273713909719068980354078852867170114|

> NOTE: Each of these files represent a single instance and are part of the same study. Also green-square and red-triangle are part of the same series, while blue-circle is in a separate series.

## Prerequisites

In order to use the DICOMWeb&trade; Standard APIs, you must have an instance of the Medical Imaging Server for DICOM deployed. If you have not already deployed the Medical Imaging Server, [Deploy the Medical Imaging Server to Azure](../quickstarts/deploy-via-azure.md).

Once you have deployed an instance of the Medical Imaging Server for DICOM, retrieve the URL for your App Service:

1. Sign into the [Azure Portal](https://portal.azure.com/).
1. Search for **App Services** and select your Medical Imaging Server for DICOM App Service.
1. Copy the **URL** of your App Service.

## Create a `DicomWebClient`

After you have deployed your Medical Imaging Server for DICOM, you will create a 'DicomWebClient'. Run the following code snippet to create `DicomWebClient` which we will be using for the rest of the tutorial. You will also need to install the fo-dicom nuget package into your console application.

```c#
string webServerUrl ="{Your DicomWeb Server URL}"
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri(webServerUrl);
DicomWebClient client = new DicomWebClient(httpClient);
```

With the `DicomWebClient` we can now perform Store, Retrieve, Search, and Delete operations.

## Store DICOM Instances (STOW)

Using the `DicomWebClient` that we have created, we can now store DICOM files.

### Store single instance

This demonstrates how to upload a single DICOM file.

_Details:_

* POST /studies

```c#
DicomFile dicomFile = DicomFile.Open(@"{Path To blue-circle.dcm}");
DicomWebResponse response = await client.StoreAsync(new[] { dicomFile });
```

### Store instances for a specific study

This  demonstrates how to upload a DICOM file into a specified study.

_Details:_

* POST /studies/{study}

```c#
DicomFile dicomFile = DicomFile.Open(@"{Path To red-triangle.dcm}");
DicomWebResponse response = await client.StoreAsync(new[] { dicomFile }, "1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420");
```

Before moving on to the next part also upload the green-square.dcm file using either of the methods above.

## Retrieving DICOM instance(s) (WADO)

The following code snippets will demonstrate how to perform each of the retrieve queries using the `DicomWebClient` created earlier.

The following variables will be used throghout the rest of the examples:

```c#
string studyInstanceUid = "1.2.826.0.1.3680043.8.498.13230779778012324449356534479549187420"; //StudyInstanceUID for all 3 examples
string seriesInstanceUid = "1.2.826.0.1.3680043.8.498.45787841905473114233124723359129632652"; //SeriesInstanceUID for green-square and red-triangle
string sopInstanceUid = "1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395"; //SOPInstanceUID for red-triangle
```

### Retrieve all instances within a study

This retrieves all instances within a single study.

_Details:_

* GET /studies/{study}

```c#
DicomWebResponse response = await client.RetrieveStudyAsync(studyInstanceUid);
```

All three of the dcm files that we uploaded previously are part of the same study so the response should return all 3 instances. Validate that the response has a status code of OK and that all three instances are returned.

### Retrieve metadata of all instances in study

This request retrieves the metadata for all instances within a single study.

_Details:_

* GET /studies/{study}/metadata

```c#
DicomWebResponse response = await client.RetrieveStudyMetadataAsync(studyInstanceUid);
```

All three of the dcm files that we uploaded previously are part of the same study so the response should return the metadata for all 3 instances. Validate that the response has a status code of OK and that all the metadata is returned.

### Retrieve all instances within a series

This request retrieves all instances within a single series.

_Details:_

* GET /studies/{study}/series/{series}

```c#
DicomWebResponse response = await client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);
```

This series has 2 instances (green-square and red-triangle), so the response should return both instances. Validate that the response has a status code of OK and that both instances are returned.

### Retrieve metadata of all instances within a series

This request retrieves the metadata for all instances within a single study.

_Details:_

* GET /studies/{study}/series/{series}/metadata

```c#
DicomWebResponse response = await client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid);
```

This series has 2 instances (green-square and red-triangle), so the response should return metatdata for both instances. Validate that the response has a status code of OK and that both instances metadata are returned.

### Retrieve a single instance within a series of a study

This request retrieves a single instances.

_Details:_

* GET /studies/{study}/series{series}/instances/{instance}

```c#
DicomWebResponse response = await client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
```

This should only return the instance red-triangle. Validate that the response has a status code of OK and that the instance is returned.

### Retrieve metadata of a single instance within a series of a study

This request retrieves the metadata for a single instances within a single study and series.

_Details:_

* GET /studies/{study}/series/{series}/instances/{instance}/metadata

```c#
DicomWebResponse response = await client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
```

This should only return the metatdata for the instance red-triangle. Validate that the response has a status code of OK and that the metadata is returned.

### Retrieve one or more frames from a single instance

This request retrieves one or more frames from a single instance.

_Details:_

* GET /studies/{study}/series/{series}/instances/{instance}/frames/{frames}

```c#
DicomWebResponse response = await client.RetrieveFramesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid,null, new[] { 1 });
```

This should return the only frame from the red-triangle. Validate that the response has a status code of OK and that the frame is returned.

## Query DICOM (QIDO)

> NOTE: Please see the [Conformance Statement](../resources/conformance-statement.md#supported-search-parameters) file for supported DICOM attributes.

### Search for studies

This request searches for one or more studies by DICOM attributes.

_Details:_

* GET /studies?StudyInstanceUID={study}

```c#
string query = $"/studies?StudyInstanceUID={studyInstanceUid}";
DicomWebResponse response = await client.QueryAsync(query);
```

Validate that response includes 1 study and that response code is OK.

### Search for series

This request searches for one or more series by DICOM attributes.

_Details:_

* GET /series?SeriesInstanceUID={series}

```c#
string query = $"/series?SeriesInstanceUID={seriesInstanceUid}";
DicomWebResponse response = await client.QueryAsync(query);
```

Validate that response includes 1 series and that response code is OK.

### Search for series within a study

This request searches for one or more series within a single study by DICOM attributes.

_Details:_

* GET /studies/{study}/series?SeriesInstanceUID={series}

```c#
string query = $"/studies/{studyInstanceUid}/series?SeriesInstanceUID={seriesInstanceUid}";
DicomWebResponse response = await client.QueryAsync(query);
```

Validate that response includes 1 series and that response code is OK.

### Search for instances

This request searches for one or more instances by DICOM attributes.

_Details:_

* GET /instances?SOPInstanceUID={instance}

```c#
string query = $"/instances?SOPInstanceUID={sopInstanceUid}";
DicomWebResponse response = await client.QueryAsync(query);
```

Validate that response includes 1 instance and that response code is OK.

### Search for instances within a study

This request searches for one or more instances within a single study by DICOM attributes.

_Details:_

* GET /studies/{study}/instances?SOPInstanceUID={instance}

```c#
string query = $"/studies/{studyInstanceUid}/instances?SOPInstanceUID={sopInstanceUid}";
DicomWebResponse response = await client.QueryAsync(query);
```

Validate that response includes 1 instance and that response code is OK.

### Search for instances within a study and series

This request searches for one or more instances within a single study and single series by DICOM attributes.

_Details:_

* GET /studies/{study}/series/{series}instances?SOPInstanceUID={instance}

```c#
string query = $"/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances?SOPInstanceUID={sopInstanceUid}";
DicomWebResponse response = await client.QueryAsync(query);
```

Validate that response includes 1 instance and that response code is OK.

## Delete DICOM

> NOTE: Delete is not part of the DICOM standard, but has been added for convenience.

### Delete a specific instance within a study and series

This request deletes a single instance within a single study and single series.

_Details:_

* DELETE /studies/{study}/series/{series}/instances/{instance}

```c#
string sopInstanceUidRed = "1.2.826.0.1.3680043.8.498.47359123102728459884412887463296905395";
DicomWebResponse response = await client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUidRed);
```

This deletes the red-triangle instance from the server. If it is successful the response status code contains no content.

### Delete a specific series within a study

This request deletes a single series (and all child instances) within a single study.

_Details:_

* DELETE /studies/{study}/series/{series}

```c#
DicomWebResponse response = await client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);
```

This deletes the green-square instance (it is the only element left in the series) from the server. If it is successful the response status code contains no content.

### Delete a specific study

This request deletes a single study (and all child series and instances).

_Details:_

* DELETE /studies/{study}

```c#
DicomWebResponse response = await client.DeleteStudyAsync(studyInstanceUid);
```

This deletes the blue-circle instance (it is the only element left in the series) from the server. If it is successful the response status code contains no content.
