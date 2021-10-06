# Extended Query Tags

## Overview

Extended query tags allows querying over DICOM tags that are not supported by the DICOMweb™ standard for [QIDO-RS](../resources/conformance-statement.md#search-qido-rs). By enabling this feature, it is possible to query against tags supported by QIDO-RS, publicly defined standard DICOM tags that are not natively supported and private tags.



## Apis

API Version: v1.0-prerelease

To help manage the supported tags in a given DICOM server instance, a few APIs are available.

| Api                                                          | Description                                  |
| ------------------------------------------------------------ | -------------------------------------------- |
| [Add Extended Query Tags](#add-extended-query-tags)          | Add extended query tag(s)                    |
| [List Extended Query Tags](#list-extended-query-tags)        | Lists metadata of all extended query tag(s)  |
| [Get Extended Query Tag](#get-extended-query-tag)            | Returns metadata of an extended query tag    |
| [Delete Extended Query Tag](#delete-extended-query-tag)      | Delete an extended query tag                 |
| [Update Extended Query Tag](#update-extended-query-tag)      | Update an extended query tag                 |
| [List Extended Query Tag Errors](#list-extended-query-tag-errors) | Lists errors on an extended query tag        |
| [Get Operation](#get-operation)                              | Returns metadata of a long-running operation |

### Add Extended Query Tags 

Add extended query tags, and starts long-running operation which reindexes DICOM instances stored in the past.

```http
POST https://{host}/extendedquerytags
```

#### URI Parameters

| Name | In   | Required | Type   | Description      |
| ---- | ---- | -------- | ------ | ---------------- |
| Host | path | True     | string | The Dicom server |

#### Request Header

| Name         | Required | Type   | Description                      |
| ------------ | -------- | ------ | -------------------------------- |
| Content-Type | True     | string | `application/json` is supported. |

#### Request Body

| Name | Required | Type                                                         | Description |
| ---- | -------- | ------------------------------------------------------------ | ----------- |
| body |          | [Extended Query Tag for Adding](#extended-query-tag-for-adding)[] |             |

#### Limitations

The following VR types are supported:

| VR   | Description           | Single Value Matching | Range Matching | Fuzzy Matching |
| ---- | --------------------- | --------------------- | -------------- | -------------- |
| AE   | Application Entity    | X                     |                |                |
| AS   | Age String            | X                     |                |                |
| CS   | Code String           | X                     |                |                |
| DA   | Date                  | X                     | X              |                |
| DS   | Decimal String        | X                     |                |                |
| FD   | Floating Point Double | X                     |                |                |
| FL   | Floating Point Single | X                     |                |                |
| IS   | Integer String        | X                     |                |                |
| LO   | Long String           | X                     |                |                |
| PN   | Person Name           | X                     |                | X              |
| SH   | Short String          | X                     |                |                |
| SL   | Signed Long           | X                     |                |                |
| SS   | Signed Short          | X                     |                |                |
| UI   | Unique Identifier     | X                     |                |                |
| UL   | Unsigned Long         | X                     |                |                |
| US   | Unsigned Short        | X                     |                |                |

> Note: Sequential tags i.e. tags under a tag of type Sequence of Items (SQ) are currently not supported.

>  Note: You can add up to 128 extended query tags.

#### Responses

| Name              | Type                                                         | Description                                                  |
| ----------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| 202 (Accepted)    | [Extended Query Tag Operation Reference](#extended-query-tag-operation-reference) | Extended query tag(s) have been added, and a long-running operation will be kicked off to reindex DICOM instances in the past. |
| 400 (Bad Request) |                                                              | Request body has invalid data.                               |
| 409 (Conflict)    |                                                              | One or more requested query tags already are supported.      |

### List Extended Query Tags

Lists metadata of all extended query tag(s).

```http
GET https://{host}/extendedquerytags
```

#### URI Parameters

| Name | In   | Required | Type   | Description      |
| ---- | ---- | -------- | ------ | ---------------- |
| Host | path | True     | string | The Dicom server |

#### Responses

| Name     | Type                                        | Description                 |
| -------- | ------------------------------------------- | --------------------------- |
| 200 (OK) | [Extended Query Tag](#extended-query-tag)[] | Returns extended query tags |

### Get Extended Query Tag

Get metadata of an extended query tag.

```http
GET https://{host}/extendedquerytags/{tagPath}
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| host    | path | True     | string | The Dicom server                                             |
| tagPath | path | True     | string | tagPath is the path for the tag. Either be tag or attribute name. E.g. `PatientId` is represented by `00100020` or `PatientId` |

####  Responses

| Name              | Type                                      | Description                                            |
| ----------------- | ----------------------------------------- | ------------------------------------------------------ |
| 200 (OK)          | [Extended Query Tag](#extended-query-tag) | Returns extended query tag                             |
| 400 (Bad Request) |                                           | Requested tag path is invalid.                         |
| 404 (Not Found)   |                                           | Extended query tag with requested tagPath is not found |

### Delete Extended Query Tag

Delete an extended query tag.

```http
DELETE https://{host}/extendedquerytags/{tagPath}
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| host    | path | True     | string | The Dicom server                                             |
| tagPath | path | True     | string | tagPath is the path for the tag. Either be tag or attribute name. E.g. `PatientId` is represented by `00100020` or `PatientId` |

#### Responses

| Name              | Type | Description                                                  |
| ----------------- | ---- | ------------------------------------------------------------ |
| 204 (No Content)  |      | Extended query tag with requested tagPath has been successfully deleted. |
| 400 (Bad Request) |      | Requested tag path is invalid.                               |
| 404 (Not Found)   |      | Extended query tag with requested tagPath is not found       |

### Update Extended Query Tag

Update an extended query tag.

```http
PATCH https://{host}/extendedquerytags/{tagPath}
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| host    | path | True     | string | The Dicom server                                             |
| tagPath | path | True     | string | tagPath is the path for the tag. Either be tag or attribute name. E.g. `PatientId` is represented by `00100020` or `PatientId` |

#### Request Header

| Name         | Required | Type   | Description                      |
| ------------ | -------- | ------ | -------------------------------- |
| Content-Type | True     | string | `application/json` is supported. |

#### Request Body

| Name | Required | Type                                                         | Description |
| ---- | -------- | ------------------------------------------------------------ | ----------- |
| body |          | [Extended Query Tag for Updating](#extended-query-tag-for-updating) |             |

#### Responses

| Name              | Type                                      | Description                                            |
| ----------------- | ----------------------------------------- | ------------------------------------------------------ |
| 20 (OK)           | [Extended Query Tag](#extended-query-tag) | Metadata of updated extended query tag                 |
| 400 (Bad Request) |                                           | Requested tag path or body is invalid                  |
| 404 (Not Found)   |                                           | Extended query tag with requested tagPath is not found |

### List Extended Query Tag Errors

Lists errors on an extended query tag.

```http
GET https://{host}/extendedquerytags/{tagPath}/errors
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| host    | path | True     | string | The Dicom server                                             |
| tagPath | path | True     | string | tagPath is the path for the tag. Either be tag or attribute name. E.g. `PatientId` is represented by `00100020` or `PatientId` |

####  Responses

| Name              | Type                                                     | Description                                            |
| ----------------- | -------------------------------------------------------- | ------------------------------------------------------ |
| 200 (OK)          | [Extended Query Tag Error](#extended-query-tag-error) [] | Lists extended query tag errors                        |
| 400 (Bad Request) |                                                          | Requested tag path is invalid.                         |
| 404 (Not Found)   |                                                          | Extended query tag with requested tagPath is not found |

### Get Operation

Get metadata of an extended query tag operation.

```http
GET https://{host}/operations/{operationId}
```

#### URI Parameters

| Name        | In   | Required | Type   | Description      |
| ----------- | ---- | -------- | ------ | ---------------- |
| host        | path | True     | string | The Dicom server |
| operationId | path | True     | string | The operation id |

#### Responses

| Name            | Type                                                         | Description                                                  |
| --------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| 200 (OK)        | [Extended Query Tag Operation](#extended-query-tag-operation) | Returns extended query tag operation which is completed      |
| 202 (Accepted)  | [Extended Query Tag Operation](#extended-query-tag-operation) | Returns extended query tag operation which has not been completed yet. |
| 404 (Not Found) |                                                              | The operation is not found                                   |

## QIDO with Extended Query Tags

When extended query tag is in `Ready` status, it can be used in [QIDO](../resources/conformance-statement.md#search-qido-rs). For example, if the tag Manufacturer Model Name (0008,1090) is added to the set of supported extended query tags, hereafter the following queries can be used to filter stored instances by Manufacturer Model Name (when tag has value on instance):

```http
../instances?ManufacturerModelName=Microsoft
```

They can also be used in conjunction with existing tags. E.g:

```http
../instances?00081090=Microsoft&PatientName=Jo&fuzzyMatching=true
```

> After extended query tag is added, any DICOM instance stored is indexed on it

#### Query Status

Extended Query Tag has attribute [QueryStatus](#extended-query-tag-status), which indicates whether allow QIDO on the tag. When reindex operation fails to process one or more DICOM instances for the tag, the tag `QueryStatus` is set to `Disabled` automatically, and you need to call [Update Extended Query Tag](#update-extended-query-tag) API to enable it if still want to use it.  In this case, we wrap erroneous tags in response header `erroneous-dicom-attributes`.

For example, extended query tag `PatientAge` has errors during reindexing, but get enabled manually.  For query below, you should be able to see header `erroneous-dicom-attributes` as `PatientAge` in response.

```http
../instances?PatientAge=035Y
```

## Definitions

### Extended Query Tag

Represents extended query tag .

| Name           | Type                                                         | Description                                                  |
| -------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Path           | string                                                       | Path of tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020. |
| VR             | string                                                       | Value representation of this tag                             |
| PrivateCreator | string                                                       | Identification code of the implementer of this private tag   |
| Level          | [Extended Query Tag Level](#extended-query-tag-level)        | Level of extended query tag                                  |
| Status         | [Extended Query Tag Status](#extended-query-tag-status)      | Status of the extended query tag                             |
| QueryStatus    | [Extended Query Tag Query Status](#extended-query-tag-query-status) | Query status of extended query tag.                          |
| Errors         | [Extended Query Tag Errors Reference](#extended-query-tag-errors-reference) | Reference to extended query tag errors                       |
| Operation      | [Extended Query Tag Operation Reference](#extended-query-tag-operation-reference) | Reference to a long-running operation                        |

**Example1:** a standard tag (0008,0070) in `Ready` status.

```json
{
        "status": "Ready",
        "level": "Instance",
        "queryStatus": "Enabled",
        "path": "00080070",
        "vr": "LO"
}
```

**Example2:**  a standard tag (0010,1010) in `Adding` status.  An operation with id `1a5d0306d9624f699929ee1a59ed57a0` is running on it, and 21 errors has occurred so far.

```json
{
        "status": "Adding",
        "level": "Study",
        "errors": {
            "count": 21,
            "href": "https://localhost:63838/extendedquerytags/00101010/errors"
        },
        "operation": {
            "id": "1a5d0306d9624f699929ee1a59ed57a0",
            "href": "https://localhost:63838/operations/1a5d0306d9624f699929ee1a59ed57a0"
        },
        "queryStatus": "Disabled",
        "path": "00101010",
        "vr": "AS"
}
```

### Extended Query Tag Operation Reference

Reference to a long-running operation.

| Name | Type   | Description          |
| ---- | ------ | -------------------- |
| Id   | string | operation id         |
| Href | string | Uri to the operation |

### Extended Query Tag Operation

Represents an extended query tag operation.

| Name            | Type                                                         | Description                                                  |
| --------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| OperationId     | string                                                       | The operation Id                                             |
| OperationType   | [Extended Query Tag Operation Type](#extended-query-tag-operation-type) | Type of  the long running operation                          |
| CreatedTime     | string                                                       | Time when the operation is created                           |
| LastUpdatedTime | string                                                       | Time when the operation is updated last time                 |
| Status          | [Extended Query Tag Operation Runtime Status](#extended-query-tag-operation-runtime-status) | Represents run time status of extended query tag operation   |
| PercentComplete | Integer                                                      | Percentage of work that has been completed by the operation  |
| Resources       | string[]                                                     | Collection of resources locations that the operation is creating or manipulating. |

**Example:** a running Reindex operation. 

```json
{
    "resources": [
        "https://localhost:63838/extendedquerytags/00101010"
    ],
    "operationId": "a99a8b51-78d4-4fd9-b004-b6c0bcaccf1d",
    "type": "Reindex",
    "createdTime": "2021-10-06T16:40:02.5247083Z",
    "lastUpdatedTime": "2021-10-06T16:40:04.5152934Z",
    "status": "Running",
    "percentComplete": 1
}
```



### Extended Query Tag Operation Runtime Status

Represents run time status of extended query tag operation

| Name       | Type   | Description                                                  |
| ---------- | ------ | ------------------------------------------------------------ |
| NotStarted | string | The operation is not started                                 |
| Running    | string | The operation is executing and has not yet finished          |
| Completed  | string | The operation has finished successfully                      |
| Failed     | string | The operation has stopped prematurely after encountering one or more errors. |

### Extended Query Tag Error

Represent error on Extended query tag.

| Name              | Type   | Description                                     |
| ----------------- | ------ | ----------------------------------------------- |
| StudyInstanceUid  | string | Study instance Uid of erroneous Dicom Instance  |
| SeriesInstanceUid | string | Series instance Uid of erroneous Dicom Instance |
| SopInstanceUid    | string | Sop instance Uid of erroneous Dicom Instance    |
| CreatedTime       | string | Time when error occured(UTC)                    |
| ErrorMessage      | string | Error message                                   |

**Example**:  an unexpected value length error on an DICOM instance. It occurred at 2021-10-06T16:41:44.4783136.

```json
{
        "studyInstanceUid": "2.25.253658084841524753870559471415339023884",
        "seriesInstanceUid": "2.25.309809095970466602239093351963447277833",
        "sopInstanceUid": "2.25.225286918605419873651833906117051809629",
        "createdTime": "2021-10-06T16:41:44.4783136",
        "errorMessage": "Value length is not expected."
}
```

### Extended Query Tag Errors Reference

Reference to extended query tag errors.

| Name  | Type    | Description                                       |
| ----- | ------- | ------------------------------------------------- |
| Count | Integer | Total number of errors on the extended query tag. |
| Href  | string  | Uri to extended query tag errors                  |

### Extended Query Tag Operation Type

The type of  extended query tag operation.

| Name    | Type   | Description                                                  |
| ------- | ------ | ------------------------------------------------------------ |
| Reindex | string | A reindexing operation that updates the indicies for previously added data based on new tags. |

### Extended Query Tag Status

The status of  extended query tag.

| Name     | Type   | Description                                                  |
| -------- | ------ | ------------------------------------------------------------ |
| Adding   | string | The extended query tag  is be adding, and an operation is reindexing DICOM instances in the past. |
| Ready    | string | The extended query tag  is ready for QIDO-RS                 |
| Deleting | string | The extended query tag  is being deleted.                    |

>  Notes: when extended query tag is added, its status is `Adding`, while a long-running operation is kicked off to reindex DICOM instances in the past, after it completes, tag status is `Ready`. 

### Extended Query Tag Level

The level of extended query tag.

| Name     | Type   | Description                                              |
| -------- | ------ | -------------------------------------------------------- |
| Instance | string | The extended query tag is relevant at the instance level |
| Series   | string | The extended query tag is relevant at the series level   |
| Study    | string | The extended query tag is relevant at the study level    |

### Extended Query Tag Query Status

The query status of extended query tag.

| Name     | Type   | Description                                         |
| -------- | ------ | --------------------------------------------------- |
| Disabled | string | The extended query tag is not allowed to be queried |
| Enabled  | string | The extended query tag is allowed to be queried     |

> Note:  Errors during reindex operation disables QIDO on the extended query tag. You can call [Update Extended Query Tag](#Update Extended Query Tag) API to enable it.

### Extended Query Tag for Updating

Represents extended query tag for updating.

| Name        | Type                                                         | Description                            |
| ----------- | ------------------------------------------------------------ | -------------------------------------- |
| QueryStatus | [Extended Query Tag Query Status](#extended-query-tag-query-status) | The query status of extended query tag |

### Extended Query Tag for Adding

Represents extended query tag for adding.

| Name           | Required | Type                                                  | Description                                                  |
| -------------- | -------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| Path           | True     | string                                                | Path of tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020. |
| VR             |          | string                                                | Value representation of this tag.  It's optional for standard tag, and required for private tag. |
| PrivateCreator |          | string                                                | Identification code of the implementer of this private tag. Only set when the tag is a private tag. |
| Level          | True     | [Extended Query Tag Level](#extended-query-tag-level) | Represents the hierarchy at which this tag is relevant. Should be one of Study,Series or Instance. |

**Example1:** a private tag (0401,1001) with VR as SS, PivateCreator as MicrosoftPC, and on Instance level.

```json
{
		"Path":"04011001",
		"VR":"SS",
		"PrivateCreator":"MicrosoftPC",
		"Level":"Instance"
}
```

**Example2:** a standard tag with attribute name as ManufacturerModelName,  VR as LO, and on Series level

```json
{
		"Path":"ManufacturerModelName", 
		"VR":"LO",
		"Level":"Series"
}
```

 **Example3: **a standard tag (0010,0040)  on Series level

```json
{
		"Path":"00100040", 
		"Level":"Study"
}
```