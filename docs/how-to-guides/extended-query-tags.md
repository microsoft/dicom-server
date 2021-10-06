# Extended Query Tags

## Overview

Extended query tags allows querying over DICOM tags that are not supported by the DICOMweb™ standard for [QIDO-RS](../resources/conformance-statement.md#search-qido-rs). By enabling this feature, it is possible to query against tags supported by QIDO-RS, publicly defined standard DICOM tags that are not natively supported and private tags.



## Apis

API Version: v1.0-prerelease

To help manage the supported tags in a given DICOM server instance, a few APIs are available.

| Api                                                     | Description                                        |
| ------------------------------------------------------- | -------------------------------------------------- |
| [Add Extended Query Tags](#Add Extended Query Tags)     | Add extended query tag(s).                         |
| [List Extended Query Tags](#List Extended Query Tags)   | Lists metadata of all extended query tag(s).       |
| [Get Extended Query Tag](#Get Extended Query Tag)       | Returns metadata of an extended query tag.         |
| [Delete Extended Query Tag](#Delete Extended Query Tag) | Delete an extended query tag.                      |
| [Update Extended Query Tag](#Update Extended Query Tag) | Update an extended query tag.                      |
| List Extended Query Tag Errors                          | Lists errors on an extended query tag.             |
| Get Operation                                           | Returns metadata of a long-time running operation. |





### Add Extended Query Tags 

Add extended query tags, and starts long-time running operation which reindexes DICOM instances stored in the past.

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

| Name           | Required | Type                                                  | Description                                                  |
| -------------- | -------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| Path           | True     | string                                                | Path of tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020. |
| VR             |          | string                                                | Value representation of this tag.  It's optional for standard tag, and required for private tag. |
| PrivateCreator |          | string                                                | Identification code of the implementer of this private tag. Only set when the tag is a private tag. |
| Level          | True     | [Extended Query Tag Level](#Extended Query Tag Level) | Represents the hierarchy at which this tag is relevant. Should be one of Study,Series or Instance. |

**Example**

```json
[
	{
		"Path":"04011001",
		"VR":"SS",
		"PrivateCreator":"MicrosoftPC",
		"Level":"Instance"
	},
	{
		"Path":"ManufacturerModelName", 
		"VR":"LO",
		"Level":"Series"
	},
	{
		"Path":"00100040", 
		"VR":"CS",
		"Level":"Study"
	},
]
```



#### Responses

| Name              | Type                    | Description                                             |
| ----------------- | ----------------------- | ------------------------------------------------------- |
| 202 (Accepted)    | [Operation](#Operation) | Extended query tag(s) have been successfully stored.    |
| 400 (Bad Request) |                         | Request body has invalid data.                          |
| 409 (Conflict)    |                         | One or more requested query tags already are supported. |





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
| 200 (OK) | [Extended Query Tag](#Extended Query Tag)[] | Returns extended query tags |

**Example**

```json
[
    {
        "status": "Ready",
        "level": "Instance",
        "errors": null,
        "operation": null,
        "queryStatus": "Enabled",
        "path": "00080070",
        "vr": "LO",
        "privateCreator": null
    },
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
        "vr": "AS",
        "privateCreator": null
    }
]
```

### 

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

 

#### Responses

| Name              | Type                                      | Description                                            |
| ----------------- | ----------------------------------------- | ------------------------------------------------------ |
| 200 (OK)          | [Extended Query Tag](#Extended Query Tag) | Returns extended query tag                             |
| 400 (Bad Request) |                                           | Requested tag path is invalid.                         |
| 404 (Not Found)   |                                           | Extended query tag with requested tagPath is not found |

**Example**

```json
{
        "status": "Ready",
        "level": "Instance",
        "errors": null,
        "operation": null,
        "queryStatus": "Enabled",
        "path": "00080070",
        "vr": "LO",
        "privateCreator": null
}
```

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
| body | True     | [Entry of Updating Extended Query Tag](#Entry of Updating Extended Query Tag) |             |

#### Responses

| Name              | Type                                      | Description                                            |
| ----------------- | ----------------------------------------- | ------------------------------------------------------ |
| 20 (OK)           | [Extended Query Tag](#Extended Query Tag) | Metadata of updated extended query tag                 |
| 400 (Bad Request) |                                           | Requested tag path or body is invalid                  |
| 404 (Not Found)   |                                           | Extended query tag with requested tagPath is not found |

**Example**

```json
{
        "status": "Ready",
        "level": "Instance",
        "errors": null,
        "operation": null,
        "queryStatus": "Enabled",
        "path": "00080070",
        "vr": "LO",
        "privateCreator": null
}
```

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

 

#### Responses

| Name              | Type                                                     | Description                                            |
| ----------------- | -------------------------------------------------------- | ------------------------------------------------------ |
| 200 (OK)          | [Extended Query Tag Error](#Extended Query Tag Error) [] | Lists extended query tag errors                        |
| 400 (Bad Request) |                                                          | Requested tag path is invalid.                         |
| 404 (Not Found)   |                                                          | Extended query tag with requested tagPath is not found |

**Example**

```json
 [
     {
        "studyInstanceUid": "2.25.253658084841524753870559471415339023884",
        "seriesInstanceUid": "2.25.309809095970466602239093351963447277833",
        "sopInstanceUid": "2.25.225286918605419873651833906117051809629",
        "createdTime": "2021-10-06T16:41:44.4783136",
        "errorMessage": "Value length is not expected."
    },
    {
        "studyInstanceUid": "2.25.196509150784672035838503876712626377778",
        "seriesInstanceUid": "2.25.213723772800486220909599220564656502366",
        "sopInstanceUid": "2.25.59253037831725331222382553080320418961",
        "createdTime": "2021-10-06T16:41:44.5163125",
        "errorMessage": "Value length is not expected."
    }
]
```

### 

## Integration with DICOMWeb™

### Querying against extended query tags

All new DICOM instances that are stored after an extended query tag is in the "Ready" state, are queryable with that tag in [QIDO](../resources/conformance-statement.md#search-qido-rs). For example, if the tag Manufacturer Model Name (0008,1090) is added to the set of supported extended query tags, hereafter the following queries can be used to filter stored instances by Manufacturer Model Name (when tag has value on instance):

```
../instances?ManufacturerModelName=Microsoft
```

```
../instances?00081090=Microsoft
```

They can also be used in conjunction with existing tags. E.g:

```
../instances?00081090=Microsoft&PatientName=Jo&fuzzyMatching=true
```

#### Search Matching

The matching types stated below are valid for extended query tags.

| Search Type | Supported VR    | Example                                                      |
| :---------- | :-------------- | :----------------------------------------------------------- |
| Range Query | Date (DA)       | {attributeID}={value1}-{value2}. For date/ time values, we supported an inclusive range on the tag. This will be mapped to `attributeID >= {value1} AND attributeID <= {value2}`. |
| Exact Match | All             | {attributeID}={value1}                                       |
| Fuzzy Match | PersonName (PN) | Matches any component of the patient name which starts with the value. |

## Limitations

Currently, only the following VR types are supported:

- Application Entity (AE)
- Age String (AS)
- Code String (CS)
- Date (DA)
- Decimal String (DS)
- Floating Point Double (FD)
- Floating Point Single (FL)
- Integer String (IS)
- Long String (LO)
- Person Name (PN)
- Short String (SH)
- Signed Long (SL)
- Signed Short (SS)
- Unique Identifier [UID] (UI)
- Unsigned Long (UL)
- Unsigned Short (US)

Sequential tags i.e. tags under a tag of type Sequence of Items (SQ) are currently not supported.

All management APIs are currently synchronous. This means that a [delete](#remove-an-extended-query-tag) request may run long as it attempts to remove any infrastructure that was put in place to support querying against the extended query tag. 

Querying against instances that were stored prior to an extended query tag being added is also not supported. Historical instances would need to be deleted and re-added to enable searching against new extended query tags.

For optimal performance, it is not recommended to store more than 100 extended query tags.



## Definitions

### Operation Reference

Reference to a long-time running operation.

| Name | Type   | Description          |
| ---- | ------ | -------------------- |
| Id   | string | operation id         |
| Href | string | Uri to the operation |

### Extended Query Tag

Extended query tag metadata.

| Name           | Type                                                         | Description                                                  |
| -------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Path           | string                                                       | Path of tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020. |
| VR             | string                                                       | Value representation of this tag                             |
| PrivateCreator | string                                                       | Identification code of the implementer of this private tag   |
| Level          | [Extended Query Tag Level](#Extended Query Tag Level)        | Level of extended query tag                                  |
| Status         | [Extended Query Tag Status](#Extended Query Tag Status)      | Status of the extended query tag                             |
| QueryStatus    | [Extended Query Tag Query Status](#Extended Query Tag Query Status) | Query status of extended query tag.                          |
| Errors         | [Extended Query Tag Errors Reference](#Extended Query Tag Errors Reference) | Reference to extended query tag errors                       |
| Operation      | [Operation Reference](#Operation Reference)                  | Reference to a long-time running operation                   |

### Extended Query Tag Error

Represent error on Extended query tag.

| Name              | Type   | Description                                     |
| ----------------- | ------ | ----------------------------------------------- |
| StudyInstanceUid  | string | Study instance Uid of erroneous Dicom Instance  |
| SeriesInstanceUid | string | Series instance Uid of erroneous Dicom Instance |
| SopInstanceUid    | string | Sop instance Uid of erroneous Dicom Instance    |
| CreatedTime       | string | Time when error happened (UTC)                  |
| ErrorMessage      | string | Error message                                   |

### Extended Query Tag Errors Reference

Reference to extended query tag errors.

| Name  | Type    | Description                                       |
| ----- | ------- | ------------------------------------------------- |
| Count | Integer | Total number of errors on the extended query tag. |
| Href  | string  | Uri to extended query tag errors                  |

### Extended Query Tag Status

The status of  extended query tag.

| Name     | Type   | Description                                                  |
| -------- | ------ | ------------------------------------------------------------ |
| Adding   | string | The extended query tag  is be adding, and an operation is reindexing DICOM instances in the past. |
| Ready    | string | The extended query tag  is ready for QIDO-RS                 |
| Deleting | string | The extended query tag  is being deleted.                    |

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



### Entry of Updating Extended Query Tag

Entry of updating extended query tag

| Name        | Type                                                         | Description                            |
| ----------- | ------------------------------------------------------------ | -------------------------------------- |
| QueryStatus | [Extended Query Tag Query Status](#Extended Query Tag Query Status) | The query status of extended query tag |