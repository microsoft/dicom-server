# Worklist (UPS-RS) Support

The DICOM service supports a subset of the Worklist Service (UPS-RS) defined in the DICOMweb&trade; Standard. Partially supported transactions include:

- [Create](#create)
- [Search](#search)
- [Request Cancellation](#request-cancellation)

The base URI for all operations below should include the [desired API version](../api-versioning.md) and the [data partition](data-partitions.md) if that feature is enabled.
Throughout, the variable `{workitem}` in a URI template stands for a Workitem UID.

You can find example requests for these transactions in the [data partition Postman collection](../resources/data-partition-feature.postman_collection.json).

## Create

This transaction uses the POST method to create a new Workitem.

| Method | Path               | Description |
| :----- | :----------------- | :---------- |
| POST   | `../workitems`         | Create a Workitem. |
| POST   | `../workitems?{workitem}` | Creates a Workitem with the specified UID. |


If not specified in the URI, the payload dataset must contain the Workitem in the SOPInstanceUID attribute.

The `Accept` and `Content-Type` headers are required in the request, and must both have the value `application/dicom+json`.

There are a number of requirements related to DICOM data attributes in the context of a specific transaction. Attributes may be
required to be present, required to not be present, required to be empty, or required to not be empty. These requirements can be 
found in [this table](https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3).

Notes on dataset attributes:
- **SOP Instance UID:** Although the reference table above says that SOP Instance UID should not be present, this guidance is specific to the DIMSE protocol and is
handled diferently in DICOMWeb&trade;. SOP Instance UID **should be present** in the dataset if not in the URI.

### Create Response Status Codes

| Code                         | Description |
| :--------------------------- | :---------- |
| 201 (Created)                | The target Workitem was successfully created. |
| 400 (Bad Request)            | There was a problem with the request. For example, the request payload did not satisfy the requirements above. |
| 401 (Unauthorized)           | The client is not authenticated. |
| 409 (Conflict)               | The Workitem already exists. |
| 415 (Unsupported Media Type) | The provided `Content-Type` is not supported. |
| 503 (Service Unavailable)    | The service is unavailable or busy. Please try again later. |

### Create Response Payload

A success response will have no payload. The `Location` and `Content-Location` response headers will contain
a URI reference to the created Workitem.

A failure response payload will contain a message describing the failure.

## Search

This transaction enables you to search for Workitems by attributes.

| Method | Path                                            | Description                       |
| :----- | :---------------------------------------------- | :-------------------------------- |
| GET    | ../workitems?                                   | Search for Workitems              |

The following `Accept` header(s) are supported for searching:

- `application/dicom+json`

### Supported Search Parameters

The following parameters for each query are supported:

| Key              | Support Value(s)              | Allowed Count | Description |
| :--------------- | :---------------------------- | :------------ | :---------- |
| `{attributeID}=` | {value}                       | 0...N         | Search for attribute/ value matching in query. |
| `includefield=`  | `{attributeID}`<br/>`all`   | 0...N         | The additional attributes to return in the response. Only top-level attributes can be specified to be included - not attributes that are part of sequences. Both public and private tags are supported. <br/>When `all` is provided, please see [Search Response](###Search-Response) for more information about which attributes will be returned for each query type.<br/>If a mixture of {attributeID} and 'all' is provided, the server will default to using 'all'. |
| `limit=`         | {value}                       | 0...1          | Integer value to limit the number of values returned in the response.<br/>Value can be between the range 1 >= x <= 200. Defaulted to 100. |
| `offset=`        | {value}                       | 0...1          | Skip {value} results.<br/>If an offset is provided larger than the number of search query results, a 204 (no content) response will be returned. |
| `fuzzymatching=` | `true` \| `false`             | 0...1          | If true fuzzy matching is applied to any attributes with the Person Name (PN) Value Representation (VR). It will do a prefix word match of any name part inside these attributes. For example, if PatientName is "John^Doe", then "joh", "do", "jo do", "Doe" and "John Doe" will all match. However "ohn" will not match. |

#### Searchable Attributes

We support searching on these attributes:

| Attribute Keyword |
| :---------------- |
| PatientName |
| PatientID |
| ReferencedRequestSequence.AccessionNumber |
| ReferencedRequestSequence.RequestedProcedureID |
| ScheduledProcedureStepStartDateTime |
| ScheduledStationNameCodeSequence.CodeValue |
| ScheduledStationClassCodeSequence.CodeValue |
| ScheduledStationGeographicLocationCodeSequence.CodeValue |
| ProcedureStepState |
| StudyInstanceUID |

#### Search Matching

We support these matching types:

| Search Type | Supported Attribute | Example |
| :---------- | :------------------ | :------ |
| Range Query | Scheduled​Procedure​Step​Start​Date​Time | {attributeID}={value1}-{value2}. For date/ time values, we support an inclusive range on the tag. This will be mapped to `attributeID >= {value1} AND attributeID <= {value2}`. If {value1} is not specified, all occurrences of dates/times prior to and including {value2} will be matched. Likewise, if {value2} is not specified, all occurrences of {value1} and subsequent dates/times will be matched. However, one of these values has to be present. `{attributeID}={value1}-` and `{attributeID}=-{value2}` are valid, however, `{attributeID}=-` is invalid. |
| Exact Match | All supported attributes | {attributeID}={value1} |
| Fuzzy Match | PatientName | Matches any component of the name which starts with the value. |

> Note: While we do not support full sequence matching, we do support exact match on the attributes listed above that are contained in a sequence.

#### Attribute ID

Tags can be encoded in a number of ways for the query parameter. We have partially implemented the standard as defined in [PS3.18 6.7.1.1.1](http://dicom.nema.org/medical/dicom/2019a/output/chtml/part18/sect_6.7.html#sect_6.7.1.1.1). The following encodings for a tag are supported:

| Value            | Example          |
| :--------------- | :--------------- |
| {group}{element} | 00100010         |
| {dicomKeyword}   | PatientName |

Example query: **../workitems?PatientID=K123&0040A370.00080050=1423JS&includefield=00404005&limit=5&offset=0**

### Search Response

The response will be an array of 0...N DICOM datasets. The following attributes are returned:

 - All attributes in [DICOM PS 3.4 Table CC.2.5-3](https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3) with a Return Key Type of 1 or 2.
 - All attributes in [DICOM PS 3.4 Table CC.2.5-3](https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3) with a Return Key Type of 1C for which the conditional requirements are met.
 - All other Workitem attributes passed as match parameters.
 - All other Workitem attributes passed as includefield parameter values.

### Search Response Codes

The query API will return one of the following status codes in the response:

| Code                      | Description |
| :------------------------ | :---------- |
| 200 (OK)                  | The response payload contains all the matching resource. |
| 206 (Partial Content)     | The response payload contains only some of the search results, and the rest can be requested through the appropriate request. |
| 204 (No Content)          | The search completed successfully but returned no results. |
| 400 (Bad Request)         | The was a problem with the request. For example, invalid Query Parameter syntax. Response body contains details of the failure. |
| 401 (Unauthorized)        | The client is not authenticated. |
| 503 (Service Unavailable) | The service is unavailable or busy. Please try again later. |

### Additional Notes

- The query API will not return 413 (request entity too large). If the requested query response limit is outside of the acceptable range, a bad request will be returned. Anything requested within the acceptable range, will be resolved.
- Paged results are optimized to return matched *newest* instance first, this may result in duplicate records in subsequent pages if newer data matching the query was added.
- Matching is case insensitive and accent insensitive for PN VR types.
- Matching is case insensitive and accent sensitive for other string VR types.
- If there is a scenario where canceling a Workitem and querying the same happens at the same time, then the query will most likely exclude the Workitem that is getting updated and the response code will be 206 (Partial Content).

## Request Cancellation

This transaction enables the user to request cancellation of a non-owned Workitem.

There are
[four valid Workitem states](https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.1.1-1):
- `SCHEDULED`
- `IN PROGRESS`
- `CANCELED`
- `COMPLETED`

This transaction will only succeed against Workitems in the `SCHEDULED` state. Any user can claim ownership of a Workitem by
setting its Transaction UID and changing its state to `IN PROGRESS`. From then on, a user can only modify the Workitem by providing
the correct Transaction UID. While UPS defines Watch and Event SOP classes that allow cancellation requests and other events to be
forwarded, this DICOM service does not implement these classes, and so cancellation requests on workitems that are `IN PROGRESS` will
return failure. An owned Workitem can be cancelled via the Change Workitem State transaction.

| Method  | Path                                            | Description                                      |
| :------ | :---------------------------------------------- | :----------------------------------------------- |
| POST    | ../workitems/{workitem}/cancelrequest           | Request the cancellation of a scheduled Workitem |

The `Content-Type` headers is required, and must have the value `application/dicom+json`.

The request payload may include Action Information as [defined in the DICOM Standard](https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.2-1).

### Request Cancellation Response Status Codes

| Code                         | Description |
| :--------------------------- | :---------- |
| 202 (Accepted)               | The request was accepted by the server, but the Target Workitem state has not necessarily changed yet. |
| 400 (Bad Request)            | There was a problem with the syntax of the request. |
| 401 (Unauthorized)           | The client is not authenticated. |
| 404 (Not Found)              | The Target Workitem was not found. |
| 409 (Conflict)               | The request is inconsistent with the current state of the Target Workitem. For example, the Target Workitem is in the SCHEDULED or COMPLETED state. |
| 415 (Unsupported Media Type) | The provided `Content-Type` is not supported. |

### Request Cancellation Response Payload

A success response will have no payload, and a failure response payload will contain a message describing the failure.
If the Workitem Instance is already in a cancelled state, the response will include the following HTTP Warning header:
`299: The UPS is already in the requested state of CANCELED.`


## Retrieve Workitem Transaction

This transaction retrieves a Workitem. It corresponds to the UPS DIMSE N-GET operation.

Refer: https://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_11.5

If the Workitem exists on the origin server, the Workitem shall be returned in an Acceptable Media Type. The returned Workitem shall not contain the Transaction UID (0008,1195) Attribute. This is necessary to preserve this Attribute's role as an access lock.

| Method  | Path                    | Description   |
| :------ | :---------------------- | :------------ |
| POST    | ../workitems/{workitem}	| Request to retrieve a Workitem Workitem			|

The `Accept` headers is required, and must have the value `application/dicom+json`.

### Retrieve Workitem Response Status Codes

| Code                         	| Description |
| :---------------------------- | :---------- |
| 200 (OK)               		| Workitem Instance was successfully retrieved. |
| 400 (Bad Request)            	| There was a problem with the request.			|
| 401 (Unauthorized)           	| The client is not authenticated. 				|
| 404 (Not Found)              	| The Target Workitem was not found. 			|

### Retrieve Workitem Response Payload

* A success response has a single part payload containing the requested Workitem in the Selected Media Type.
* The returned Workitem shall not contain the Transaction UID (0008,1195) Attribute of the Workitem, since that should only be known to the Owner.
* A failure response payload may contain a Status Report describing any failures, warnings, or other useful information.

