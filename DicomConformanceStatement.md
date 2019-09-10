# DICOM Conformance Statement

The **Azure for Health API** supports a subset of the DICOM Web standard. Support includes:

- [Store Transaction](##Store-Transaction)
- [Retrieve Transaction](##Retrieve-Transaction)
- [Search Transaction](##Search-Transaction)

## Store Transaction

This transaction uses the POST method to Store representations of Studies, Series, and Instances contained in the request payload.

Method|Path|Description
----------|----------|----------
POST|../studies|Store instances.
POST|../studies/{studyInstanceUID}|Store instances for a specific study. If an instance does not belong to the provided study identifier, that specific instance will be rejected with a '`43265`' warning code.

The following `'Accept'` headers for the response are supported:
- `application/dicom+json`

The following `'Content-Type'` headers are supported:
- `multipart/related; type=application/dicom`

> Note: The Server will <u>not</u> coerce or replace attributes that conflict with existing data. All data will be stored as provided.

The following DICOM elements are required to be present in every DICOM file attempting to be stored:
- StudyInstanceUID
- SeriesInstanceUID
- SopInstanceUID

> Note: All identifiers must be between 1 and 64 characters long, and only contain alpha numeric characters or the following special characters: '.', '-'.

Each file stored must have a unique combination of StudyInstanceUID, SeriesInstanceUID and SopInstanceUID. The warning code `45070` will be returned in the result if a file with the same identifiers exists.

### Response Status Codes

Code|Description
----------|----------
200 (OK)|When all the SOP instances in the request have been stored.
202 (Accepted)|When some instances in the request have been stored but others have failed.
204 (No Content)|No content was provided in the store transaction request.
400 (Bad Request)|The request was badly formatted. For example, the provided study instance identifier did not conform the expected UID format.
406 (Not Acceptable)|The specified `Accept` header is not supported.
409 (Conflict) |When none of the instances in the store transaction request have been stored.
415 (Unsupported Media Type)|The provided `Content-Type` is not supported.

### Response Payload

The response payload will populate a DICOM dataset with the following elements:

Tag|Name|Description
----------|----------|----------
(0008, 1190)|RetrieveURL|The Retrieve URL of the study if the StudyInstanceUID was provided in the store request.
(0008,1198)|FailedSOPSequence|The sequence of instances that failed to store.
(0008, 1199)|ReferencedSOPSequence|The sequence of stored instances.

Each dataset in the `FailedSOPSequence` will have the following elements (if the DICOM file attempting to be stored could be read):

Tag|Name|Description
----------|----------|----------
(0008, 1150)|ReferencedSOPClassUID|The SOP class unique identifier of the instance that failed to store.
(0008, 1150)|ReferencedSOPInstanceUID|The SOP instance unique identifier of the instance that failed to store.
(0008,1197)|FailureReason|The reason code why this instance failed to store

Each dataset in the `ReferencedSOPSequence` will have the following elements:

Tag|Name|Description
----------|----------|----------
(0008, 1150)|ReferencedSOPClassUID|The SOP class unique identifier of the instance that failed to store.
(0008, 1150)|ReferencedSOPInstanceUID|The SOP instance unique identifier of the instance that failed to store.
(0008,1190)|RetrieveURL|The retrieve URL of this instance on the DICOM server.

An example response with `Accept` header `application/dicom+json`:

```json
{
  "00081190":
  {
    "vr":"UR",
    "Value":["http://localhost/studies/d09e8215-e1e1-4c7a-8496-b4f6641ed232"]
  },
  "00081198":
  {
    "vr":"SQ",
    "Value":
    [{
      "00081150":
      {
        "vr":"UI","Value":["cd70f89a-05bc-4dab-b6b8-1f3d2fcafeec"]
      },
      "00081155":
      {
        "vr":"UI",
        "Value":["22c35d16-11ce-43fa-8f86-90ceed6cf4e7"]
      },
      "00081197":
      {
        "vr":"US",
        "Value":[43265]
      }
    }]
  },
  "00081199":
  {
    "vr":"SQ",
    "Value":
    [{
      "00081150":
      {
        "vr":"UI",
        "Value":["d246deb5-18c8-4336-a591-aeb6f8596664"]
      },
      "00081155":
      {
        "vr":"UI",
        "Value":["4a858cbb-a71f-4c01-b9b5-85f88b031365"]
      },
      "00081190":
      {
        "vr":"UR",
        "Value":["http://localhost/studies/d09e8215-e1e1-4c7a-8496-b4f6641ed232/series/8c4915f5-cc54-4e50-aa1f-9b06f6e58485/instances/4a858cbb-a71f-4c01-b9b5-85f88b031365"]
      }
    }]
  }
}
```

### Failure Reason Codes

Code|Description
----------|----------
272|The store transaction did not store the instance because of a general failure in processing the operation.
43265|The provided instance StudyInstanceUID did not match the specified StudyInstanceUID in the store request.
45070|A DICOM file with the same StudyInstanceUID, SeriesInstanceUID and SopInstanceUID has already been stored. If you wish to update the contents, delete this instance first.

## Retrieve Transaction

This Retrieve Transaction offers support for retrieving stored studies, series, instances and frames by reference.

The **Azure for Health** API supports the following methods:

Method|Path|Description
----------|----------|----------
GET|../study/{study}|Retrieves an entire study.
GET|../study/{study}/metadata|Retrieves all the metadata for every instance in the study.
GET|../study/{study}/series/{series}|Retrieves an series.
GET|../study/{study}/series/{series}/metadata|Retrieves all the metadata for every instance in the series.
GET|../study/{study}/series/{series}/instances/{instance}|Retrieves a single instance.
GET|../study/{study}/series/{series}/instances/{instance}/metadata|Retrieves the metadata for a single instance.
GET|../study/{study}/series/{series}/instances/{instance}/frames/{frames}|Retrieves one or many frames from a single instance. To specify more than one frame, a comma seperate each frame to return, e.g. /study/1/series/2/instance/3/frames/4,5,6

### Retrieve Study or Series
The following `'Accept'` headers are supported for retrieving study or series:
- `multipart/related; type="application/dicom"; transfer-syntax=1.2.840.10008.1.2.1 (default)`
- `multipart/related; type="application/dicom"; transfer-syntax=*`

### Retrieve Metadata (for Study/ Series/ or Instance)
The following `'Accept'` headers are supported for retrieving metadata for a study, series or single instance:
- `application/dicom+json (default)`

Retrieving metadata will not return attributes with the following value representations:
VR Name|Full
----------|----------
OB|Other Byte
OD|Other Double
OF|Other Float
OL|Other Long
OV|Other Long
OV|Other 64-Bit Very Long
OW|Other Word
UN|Unkown


### Retrieve Frames
The following `'Accept'` headers are supported for retrieving frames:
- `multipart/related; type="application/octet-stream"; transfer-syntax=1.2.840.10008.1.2.1 (default)`
- `multipart/related; type="application/octet-stream"; transfer-syntax=*`

> If the `'transfer-syntax'` header is not set, the Retrieve Transaction will default to 1.2.840.10008.1.2.1 (Little Endian Explicit). <br/> It is worth noting that if a file was uploaded using a compressed transfer syntax, by default, the result will be re-encoded. This could reduce the performance of the DICOM server on 'retrieve'. In this case, it is recommended to set the `transfer-syntax` header to **'`*`'**, or store all files as Little Endian explicit.

### Response Status Codes

Code|Description
----------|----------
200 (OK)|All requested data has been retrieved.
400 (Bad Request)|The request was badly formatted. For example, the provided study instance identifier did not conform the expected UID format or the requested transfer-syntax encoding is not supported.
404 (Not Found)|The specified DICOM resource could not be found.

## Search Transaction
Query based on ID for DICOM Objects (QIDO) enables you to search for studies, series and instances by attributes. More detail can be found in [PS3.18 6.7](http://dicom.nema.org/medical/dicom/2019a/output/chtml/part18/sect_6.7.html).

The **Azure for Health API** supports the following **HTTP GET** endpoints:

Method|Path|Description
----------|----------|----------
*Search for Studies*|
GET|../studies?...|Search for studies.|
*Search for Series*|
GET|../series?...|Search for series.
GET|../studies/{study}/series?...|Search for series in a study.
*Search for Instances*|
GET|../instances?...|Search for instances.
GET|../studies/{study}/instances?...|Search for instances in a study.
GET|../studies/{study}/series/{series}/instances?...|Search for instances in a series.

Accept Header Supported:
  - `application/dicom+json`

### Supported Search Parameters
The following parameters for each query are supported:

Key|Support Value(s)|Allowed Count|Description
----------|----------|----------|----------
`{attributeID}=`|{value}|0...N|Search for attribute/ value matching in query.
`includefield=`|`{attributeID}`<br/>'`all`'|0...N|The additional attributes to return in the response.<br/>When '`all`' is provided, please see [Search Response](###Search-Response) for more information about which attributes will be returned for each query type.<br/>If a mixture of {attributeID} and 'all' is provided, the server will default to using 'all'.
`limit=`|{value}|0..1|Integer value to limit the number of values returned in the response.<br/>Value can be between the range 1 >= x <= 100.
`offset=`|{value}|0..1|Skip {value} results.<br/>If an offset is provided larger than the number of search query results, a 204 (no content) response will be returned.

#### Search Parameters
By default, the following DICOM attributes can be used for searching: 

Attribute Name|Tag
----------|----------
Study Date|(0008, 0020)
Accession Number|(0008, 0050)
Modality|(0008, 0060)
Modalities In Study|(0008, 0061)
Referring Physician Name|(0008, 0090)
Patient Name|(0010, 0010)
Patient ID|(0010, 0020)


Based on the value representation of the tag, the Azure for Health API can also support exact matching or range querying:

Search Type|Supported Value Representation(s)|Example|Description
----------|----------|----------|----------|----------
Range Query|DA (Date)<br/>DT (Date Time)<br/>TM (Time)|{attributeID}={value1}-{value2}|For date/ time values, we supported an inclusive range on the tag. This will be mapped to `attributeID >= {value1} AND attributeID <= {value2}`.
Exact Match|AE (Application Entity)<br/>AS (Age String)<br/>AT (Attribute Tag)<br/>CS (Code String)<br/>DA (Date)<br/>Decimal String (DS)<br/>DT (Date Time)<br/>FL (Floating Point Single)<br/>FD (Floating Point Double)<br/>IS (Integer String)<br/>LO (Long String)<br/>LT (Long Text)<br/>PN (Person Name)<br/>SH (Short String)<br/>SL (Signed Long)<br/>SS (Signed Short)<br/>ST (Short Text)<br/> TM (Time)<br/>UI (Unqiue Identifer - UID)|{attributeID}={value1}|This is a straight-forward exact match of the element value. As some DICOM tags have a value multiplicity greater than 1, where applicable, the search will check all values using an `ARRAY_CONTAINS`.

#### Attribute ID

Tags can be encoded in a number of ways for the query parameter. We have implemented the standard as defined in [PS3.18 6.7.1.1.1](http://dicom.nema.org/medical/dicom/2019a/output/chtml/part18/sect_6.7.html#sect_6.7.1.1.1). The following encodings for a tag are supported:

Value|Example
----------|----------
{group}{element}|0020000D
{dicomKeyword}|StudyInstanceUID
{sequence}.{attribute}|OtherPatientIDsSequence.PatientID or 00101002.00100020

Example query searching for instances: **../instances?modality=CT&00280011=512&includefield=00280010&limit=5&offset=0**

### Unsupported Query Paramters
The following parameters noted in the DICOM web standard are not currently supported:

Key|Value|Description
----------|----------|----------
`fuzzymatching=`|true or false|Whether query should use fuzzy matching on the provided {attributeID}/{value} pairs.

Querying using the `TimezoneOffsetFromUTC` (`00080201`) attribute is also not supported.

### Search Response

The response will be an array of DICOM datasets. Depending on the search type, the below attributes will be returned, based on the [IHE standard](https://www.ihe.net/uploadedFiles/Documents/Radiology/IHE_RAD_TF_Vol2.pdf).

When an include field is requested in the query (for study and series searches), it must be one of the below attributes, or it will be ignored in the query. If `includefield=all` is provided all of the mentioned attributes will be returned.

#### Study Search
*Required Attributes:* 
Attribute Name|Tag
----------|----------
Specific Character Set|(0008, 0005)
Study Date|(0008, 0020)
Study Time|(0008, 0030)
Accession Number|(0008, 0050)
Patient Name|(0010, 0010)
Patient ID|(0010, 0020)
Study ID|(0020, 0010)
Study Instance UID|(0020, 000D)
Modalities In Study|(0008, 0061)
Referring Physician Name|(0008, 0090)
Patient Birth Date|(0010, 0030)
Patient Sex|(0010, 0040)
Number Of Study Related Series|(0020, 1206)
Number Of Study Related Instances|(0020, 1208)
Timezone Offset From UTC|(0008, 0201)
Retrieve URL|(0008, 1190)
Instance Availability|(0008, 0056)

*Optional Attributes:*
Attribute Name|Tag
----------|----------
Person Identification Code Sequence|(0040, 1101)
Person Address|(0040, 1102)
Person Telephone Numbers|(0040, 1103)
Person Telecom Information|(0040, 1104)
Institution Name|(0008, 0080)
Institution Address|(0008, 0081)
Institution Code Sequence|(0008, 0082)
Referring Physician Identification Sequence|(0008, 0096)
Consulting Physician Name|(0008, 009C)
Consulting Physician Identification Sequence|(0008, 009D)
Issuer Of Accession Number Sequence|(0008, 0051)
Local Namespace Entity ID|(0040, 0031)
Universal Entity ID|(0040, 0032)
Universal Entity ID Type|(0040, 0033)
Study Description|(0008, 1030)
Physicians Of Record|(0008, 1048)
Physicians Of Record Identification Sequence|(0008, 1049)
Name Of Physicians Reading Study|(0008, 1060)
Physicians Reading StudyIdentification Sequence|(0008, 1062)
Requesting Service Code Sequence|(0032, 1034)
Referenced Study Sequence|(0008, 1110)
Procedure Code Sequence|(0008, 1032)
Reason For Performed Procedure Code Sequence|(0040, 1012)

#### Series Search:
*Required Attributes:*
Attribute Name|Tag
----------|----------
Study Instance UID|(0020, 000D)
Modality|(0008, 0060)
Series Number|(0020, 0011)
Series Instance UID|(0020, 000E)
Number Of Series Related Instances|(0020, 1209)
Series Description|(0008, 103E) 
Requested Procedure ID|(0040, 1001)
Scheduled Procedure Step ID|(0040, 0009)
Performed Procedure Step Start Date|(0040, 0244)
Performed Procedure Step Start Time|(0040, 0245)
Body Part Examined|(0018, 0015)
Specific Character Set|(0008, 0005)
Timezone Offset From UTC|(0008, 0201)
Retrieve URL|(0008, 1190)

*Optional Attributes:*
Attribute Name|Tag
----------|----------
Laterality|(0020, 0060)
SeriesDate|(0008, 0021)
SeriesTime|(0008, 0031)
Performed Procedure Step ID|(0040, 0253)
Referenced SOP Class UID|(0008, 1155)
Referenced SOP Instance UID|(0008, 1155)

#### Instance Search

*Required Attributes:*
Attribute Name|Tag
----------|----------
Study Instance UID|(0020, 000D)
Series Instance UID|(0020, 000E)
Instance Number|(0020, 0013)
SOP Instance UID|(0008, 0018)
SOP Class UID|(0008, 0016)
Rows|(0028, 0010)
Columns|(0028, 0011)
Bits Allocated|(0028, 0100)
Number Of Frames|(0028, 0008)
Specific Character Set|(0008, 0005)
Timezone Offset From UTC|(0008, 0201)
Retrieve URL|(0008, 1190)

*Optional Attributes:*

All attributes available in the DICOM instance expect for any of the following value representations: OB, OD, OF, OL, OW, or UN.

### Response Codes

The query API will return one of the following status codes in the response:

Code|Name|Description
----------|----------|----------
*Success*|
200|OK|Whether query should use fuzzy matching on the provided {attributeID}/{value} pairs.
204|No Content|The search completed successfully but returned no results.
*Failure*|
400|Bad Request|The QIDO-RS Provider was unable to perform the query because the Service Provider cannot understand the query component.
401|Unauthorized|The QIDO-RS Provider refused to perform the query because the client is not authenticated.
403|Forbidden|The QIDO-RS provider understood the request, but is refusing to perform the query (e.g. an authenticated user with insufficient privileges).
503|Busy|Service is unavailable

The query API will not return 413 (request entity too large). If the requested query response limit is outside of the acceptable range, a bad request will be returned. Anything requested within the acceptable range, will be resolved.

### Warning Codes

When the API returns information related to the query response, the HTTP response **Warning Header** will be populated with a list of codes and a description. All known warning codes are provided below:

Code|Description
----------|----------
299 {+Service}: There are additional results that can be requested.|The provided query resulted in more results, but has been limited based on the query limits or internal default limits.
299 {+Service}: The fuzzy matching parameter is not supported. Only literal matching has been performed.|Making a request, passing the parameter ?fuzzymatching={value}, will cause this header to be returned.
299 {+Service}: The results of this query have been coalesced because the underlying data has inconsistencies across the queried instances.|The executed query return results that had inconsistent tags at the instance level. A decision has been taken by the server how to merge the inconsistent tags, but this might not be expected by the caller.

### Inconsistent DICOM Tags

It is possible when searching for a study or series, the DICOM tags are inconsistent between the individual instances. The Azure for Health API will allow searching on all inconsistent tags, and aim to provide a consistent behaviour for each search response.

As an example, different instances in the same study could have been created with inconsistent study dates. When this happens, the API will allow searching on inconsistent tags, and return the tag that best matches your query. For an example:

Instance 1
  - Study Instance UID (0020, 000D) = 5
  - Study Date (0008, 0020) = 20190505

Instance 2
  - Study Instance UID (0020, 000D) = 5
  - Study Date (0008, 0020) = 20190510

QIDO Search 1: **../studies?0020000D=5&0020000D=20190510**

Returns:
  - Study Instance UID (0020, 000D) = 5
  - Study Date (0008, 0020) = 20190510

QIDO Search 2: **../studies?0020000D=5&0020000D=20190504-20190507**

Returns:
  - Study Instance UID (0020, 000D) = 5
  - Study Date (0008, 0020) = 20190505

When the query matches both inconsistent tags, one of the matched tags will be consistently chosen; repeated searches will return the same result.

