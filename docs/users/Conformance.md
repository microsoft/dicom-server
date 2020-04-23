# DICOM Conformance Statement

> This is currently a work-in progress document
> 

The **Azure for Health API** supports a subset of the DICOM Web standard. Support includes:

- [Store (STOW-RS)](#store-(stow-rs))
- [Retrieve (WADO-RS)](#retrieve-(wado-rs))
- [Search (QIDO-RS)](#search-(qido-rs))

Additionally, the following non-standard APIs are supported:

- [Delete](#delete)

## Store (STOW-RS)

Store Over the Web (STOW) enables you to store specific instances to the server. The specification for WADO-RS can be found in [PS3.18 10.5](http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_10.5.html).
**Important** STOW-RS will only support storing entire series. If the same series is posted multiple times the behaviour is to override.

Method|Path|Description
----------|----------|----------
POST|../studies|Store instances
POST|../studies/{studyInstanceUID}|Store instances for a specific study. If any instance does not belong to the studyInstanceUID it will be rejected

- Accept Header Supported: `application/dicom+json`
- Content-Type: `multipart/related; type=application/dicom`

### Dicom store semantics

- Stored DICOM files should at least have the following tags:
  - SOPInstanceUID
  - SeriesInstanceUID
  - StudyInstanceUID
  - SopClassUID
  - PatientID
- No coercing or replacing of attributes is done by the server

### Response

Code|Name|Description
----------|----------|----------
200 | OK | When all the SOP instances in the request have been stored
202 | Accepted | When some instances in the request have been stored
204 | No Content | 
400 | Bad Request| 
406 | Not Acceptable |
409 | Conflict | When none of the instances in the request have been stored
415 | Unsupported Media Type |
429 | Too many requests | reached the limit of a request. Need to implement


- Content-Type: `application/dicom+json`
- DicomDataset:
  - Retrieve URL (0008,1190)
  - Failed SOP Sequence (0008,1198)
    - Referenced SOP Class UID (0008,1150)
    - Referenced SOP Instance UID (0008,1155)
    - Failure Reason (0008,1197)
  - Referenced SOP Sequence (0008,1199)
    - Referenced SOP Class UID (0008,1150)
    - Referenced SOP Instance UID (0008,1155)
    - Retrieve URL (0008,1190)
<br/>
<br/>
- Accept header application/dicom+xml is not supported.
- Separate Metadata and Bulk data part requests are not supported.

## Retrieve (WADO-RS)
Web Access to DICOM Objects (WADO) enables you to retrieve specific studies, series and instances by reference. The specification for WADO-RS can be found in [PS3.18 6.5](http://dicom.nema.org/medical/dicom/2019a/output/chtml/part18/sect_6.5.html). WADO-RS can return binary DICOM instances, metadata as well as rendered instances.

Following **HTTP GET** endpoints are supported:

Method|Path|Description|Accept Header
----------|----------|----------|----------
DICOM|
GET|../studies/{study}|Retrieve full study|application/dicom, application/octet-stream
GET|../studies/{study}/series/{series}|Retrieve full series|application/dicom, application/octet-stream
GET|../studies/{study}/series/{series}/instances/{instance}|Retrieve instance|application/dicom, application/octet-stream
GET|../studies/{study}/series/{series}/instances/{instance}/frames/{frames}|Retrieve frames|application/octet-stream
Metadata|
GET|../studies/{study}/metadata|Retrieve full study metadata|application/dicom+json
GET|../studies/{study}/series/{series}/metadata|Retrieve full series metadata|application/dicom+json
GET|../studies/{study}/series/{series}/instances/{instance}/metadata|Retrieve instance metadata|application/dicom+json

### Supported transfer syntax for Retrieve DICOM  (*check with fo-dicom)

- 1.2.840.10008.1.2 (Little Endian Implicit)
- 1.2.840.10008.1.2.1 (Little Endian Explicit)
- 1.2.840.10008.1.2.1.99 (Deflated Explicit VR Little Endian)
- 1.2.840.10008.1.2.2 (Explicit VR Big Endian)
- 1.2.840.10008.1.2.4.50 (JPEG Baseline Process 1)
- 1.2.840.10008.1.2.4.51 (JPEG Baseline Process 2 & 4)
- 1.2.840.10008.1.2.4.90 (JPEG 2000 Lossless Only)
- 1.2.840.10008.1.2.4.91 (JPEG 2000)
- 1.2.840.10008.1.2.5 (RLE Lossless)
- \* 

A transfer syntax of * means no transcoding will be done, so the transfer syntax of the uploaded file will be used. This is the fastest way to retrieve.
Retrieve Metadata does not return any attribute which has a DICOM Value Representation of OB, OD, OF, OL, OW, or UN.

### Response

Code|Name|Description
----------|----------|----------
200 | OK | Response contains all of the requested resources 
400 | Bad Request| Invalid request
404 | Not Found| Requested resource does not exist
406 | Not Acceptable | Server does not support the acceptable media type
415 | Unsupported Media Type | Server does not support the transfer syntax

BulkData, Thumbnails and Rendered query parameters is not supported.

## Search (QIDO-RS)

Query based on ID for DICOM Objects (QIDO) enables you to search for studies, series and instances by attributes. More detail can be found in [PS3.18 6.7](http://dicom.nema.org/medical/dicom/2019a/output/chtml/part18/sect_6.7.html).

Following **HTTP GET** endpoints are supported:

Method|Path|Description
----------|----------|----------
*Search for Studies*|
GET|../studies?...|Search for studies|
*Search for Series*|
GET|../series?...|Search for series
GET|../studies/{study}/series?...|Search for series in a study
*Search for Instances*|
GET|../instances?...|Search for instances
GET|../studies/{study}/instances?...|Search for instances in a study
GET|../studies/{study}/series/{series}/instances?...|Search for instances in a series

Accept Header Supported: `application/dicom+json`

### Supported Query Parameters
The following parameters for each query are supported:

Key|Support Value(s)|Allowed Count|Description
----------|----------|----------|----------
`{attributeID}=`|{value}|0...N|Search for attribute/ value matching in query.
`includefield=`|`{attributeID}`<br/>'`all`'|0...N|The additional attributes to return in the response.<br/>When '`all`' is provided, please see [Search Response](###Search-Response) for more information about which attributes will be returned for each query type.<br/>If a mixture of {attributeID} and 'all' is provided, the server will default to using 'all'.
`limit=`|{value}|0..1|Integer value to limit the number of values returned in the response.<br/>Value can be between the range 1 >= x <= 200. Defaulted to 100.
`offset=`|{value}|0..1|Skip {value} results.<br/>If an offset is provided larger than the number of search query results, a 204 (no content) response will be returned.
`fuzzymatching=`|true\|false|0..1|If true fuzzy matching is applied to PatientName attribute. It will do a prefix word match of any name part inside PatientName value.

#### Search Parameters
We support searching on below attributes and search type.

- Studies:
    - StudyInstanceUID
    - PatientName
    - PatientID
    - AccessionNumber
    - ReferringPhysicianName
    - StudyDate
    - StudyDescription
   
- Series: all study level search terms and
    - SeriesInstanceUID
    - Modality
    - PerformedProcedureStepStartDate
- Instances: all study/series level search terms and
    - SOPInstanceUID

    
Search Type|Supported Attribute|Example|
----------|----------|----------|----------|----------
Range Query|StudyDate|{attributeID}={value1}-{value2}|For date/ time values, we supported an inclusive range on the tag. This will be mapped to `attributeID >= {value1} AND attributeID <= {value2}`.
Exact Match|All supported Atrributes |{attributeID}={value1}
Fuzzy Match|PatientName|Matches any component of the patientname which starts with the value

#### Attribute ID

Tags can be encoded in a number of ways for the query parameter. We have partially implemented the standard as defined in [PS3.18 6.7.1.1.1](http://dicom.nema.org/medical/dicom/2019a/output/chtml/part18/sect_6.7.html#sect_6.7.1.1.1). The following encodings for a tag are supported:

Value|Example
----------|----------
{group}{element}|0020000D
{dicomKeyword}|StudyInstanceUID

Example query searching for instances: **../instances?Modality=CT&00280011=512&includefield=00280010&limit=5&offset=0**

*We will support expanding the search attributes by customization in the future.

Querying using the `TimezoneOffsetFromUTC` (`00080201`) is not supported.

### Search Response

The response will be an array of DICOM datasets. Depending on the resource , by *default* the following attributes are returned:

#### Study:
Attribute Name|Tag
----------|----------
Specific Character Set|(0008, 0005)
Study Date|(0008, 0020)
Study Time|(0008, 0030)
Accession Number|(0008, 0050)
Instance Availability|(0008, 0056)
Referring Physician Name|(0009, 0090)
Timezone Offset From UTC|(0008, 0201)
Patient Name|(0010, 0010)
Patient ID|(0010, 0020)
Patient Birth Date|(0010, 0030)
Patient Sex|(0010, 0040)
Study ID|(0020, 0010)
Study Instance UID|(0020, 000D)

#### Series:
Attribute Name|Tag
----------|----------
Specific Character Set|(0008, 0005)
Modality|(0008, 0060)
Timezone Offset From UTC|(0008, 0201)
Series Description|(0008, 103E) 
Series Instance UID|(0020, 000E)
Performed Procedure Step Start Date|(0040, 0244)
Performed Procedure Step Start Time|(0040, 0245)
Request Attributes Sequence|(0040, 0275)

#### Instance:
Attribute Name|Tag
----------|----------
Specific Character Set|(0008, 0005)
SOP Class UID|(0008, 0016)
SOP Instance UID|(0008, 0018)
Instance Availability|(0008, 0056)
Timezone Offset From UTC|(0008, 0201)
Instance Number|(0020, 0013)
Rows|(0028, 0010)
Columns|(0028, 0011)
Bits Allocated|(0028, 0100)
Number Of Frames|(0028, 0008)

If includefield=all, blew attributes are included along with default attributes. Along with default attributes, this is the full list of attributes supported at each resource level.

#### Study:
Attribute Name|
----------|
Study Description|
Anatomic Regions In Study Code Sequence|
Procedure Code Sequence|
Name Of Physicians Reading Study|
Admitting Diagnoses Description|
Referenced Study Sequence|
Patient Age|
Patient Size|
Patient Weight|
Occupation|
Additional Patient History|

#### Series:
Attribute Name|
----------|
Series Number|
Laterality|
Series Date|
Series Time|

Along with those below attributes are returned
- All the match query parameters and UIDs in the resource url.
- IncludeField attributes supported at that resource level. Not supported attributes will not be returned.
- If the target resource is All Series, then Study level attributes are also returned.
- If the target resource is All Instances, then Study and Series level attributes are also returned.
- If the target resource is Study's Instances, then Series level attributes are also returned.

### Response Codes

The query API will return one of the following status codes in the response:

Code|Name|Description
----------|----------|----------
*Success*|
200|OK|The response payload contains all the matching resource.
204|No Content|The search completed successfully but returned no results.
*Failure*|
400|Bad Request|The server was unable to perform the query because the query component was invalid. Response body contains details of the failure.
401|Unauthorized|The server refused to perform the query because the client is not authenticated.
503|Busy|Service is unavailable

- The query API will not return 413 (request entity too large). If the requested query response limit is outside of the acceptable range, a bad request will be returned. Anything requested within the acceptable range, will be resolved.
- When target resource is Study/Series there is a potential for inconsistent study/series level metadata across multiple instances. For example, two instances could have different patientName. In this case we will return the study of either of the patientName match.
- Paged results are optimized to return matched *newest* instance first, this may result in duplicate records in subsequent pages if newer data matching the query was added.
- Matching on the strings is case in-sensitive and accent sensitive.

## Delete

The following **HTTP DELETE** endpoints will be supported:

Method    | Path                                                     | Description
----------|----------------------------------------------------------|-------------------------
DELETE    | ../studies/{study}                                       | Delete entire study
DELETE    | ../studies/{study}/series/{series}                       | Delete entire series
DELETE    | ../studies/{study}/series/{series}/instances/{instance}  | Delete entire instance


### Response Codes

The Delete API will return one of the following status codes in the response:

Code      | Name         | Description
----------|--------------| --------------------------------------------
204       | No content   | Requested resource was successfully deleted.
400       | Bad request  | The request was invalid.
404       | Not found    | The specified object wasn't found
