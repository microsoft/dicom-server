# Extended Query Tags

## Overview

Extended query tags allows querying over DICOM tags that are not supported by the DICOMweb™ standard for [QIDO-RS](../resources/conformance-statement.md#search-qido-rs). By enabling this feature, it is possible to query against tags supported by QIDO-RS, publicly defined standard DICOM tags that are not natively supported and private tags.



## Apis

API Version: v1.0-prerelease

To help manage the supported tags in a given DICOM server instance, a few APIs are available.

| Api                                                   | Description                                        |
| ----------------------------------------------------- | -------------------------------------------------- |
| [Add Extended Query Tags](#Add Extended Query Tags)   | Add extended query tag(s).                         |
| [List Extended Query Tags](#List Extended Query Tags) | Lists metadata of all extended query tag(s).       |
| Delete Extended Query Tag                             | Delete an extended query tag.                      |
| Get Extended Query Tag                                | Returns metadata of an extended query tag.         |
| Update Extended Query Tag                             | Update an extended query tag.                      |
| Get Extended Query Tag Errors                         | Returns errors for an extended query tag.          |
| Get Operation                                         | Returns metadata of a long-time running operation. |
|                                                       |                                                    |





### Add Extended Query Tags 

Add extended query tags, and starts long-time running operation which reindexes DICOM instances stored in the past.

```http
POST https://{Host}/extendedquerytags
```



<h4>URI Parameters</h4>

| Name | In   | Required | Type   | Description      |
| ---- | ---- | -------- | ------ | ---------------- |
| Host | path | True     | string | The Dicom server |



<h4>Request Header</h4>

| Name         | Required | Type   | Description                      |
| ------------ | -------- | ------ | -------------------------------- |
| Content-Type | True     | string | `application/json` is supported. |



#### Request Body

| Name           | Required | Type   | Description                                                  |
| -------------- | -------- | ------ | ------------------------------------------------------------ |
| Path           | True     | string | Path of tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020. |
| VR             |          | string | Value representation of this tag.  It's optional for standard tag, and required for private tag. |
| PrivateCreator |          | string | Identification code of the implementer of this private tag. Only set when the tag is a private tag. |
| Level          | True     | string | Represents the hierarchy at which this tag is relevant. Should be one of Study,Series or Instance. |

<h5>Example</h5>

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

List all supported extended query tags with route: /extendedquerytags

#### Response

```
[
	{
		"Path":"04011001",
		"VR":"SS",
		"PrivateCreator":"MicrosoftPC",
		"Level":"Instance",
		"Status":"Adding"
	},
	{
		"Path":"00081090",
		"VR":"LO",
		"Level":"Series",
		"Status":"Ready"
	},
	{
		"Path":"00100040",
		"VR":"CS",
		"Level":"Study",
		"Status":"Deleting"
	},
]
```

### Response status codes

| Code     | Description                                          |
| -------- | ---------------------------------------------------- |
| 200 (OK) | Extended query tags have been successfully returned. |

### Get an extended query tag

Detail an extended query tag's metadata using the route: /extendedquerytags/{tagPath}

#### Parameter

tagPath is the path for the tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020.

#### Response

```
{
    "Path":"04011001",
    "VR":"SS",
    "PrivateCreator":"MicrosoftPC",
    "Level":"Instance",
    "Status":"Adding"
}
```

#### Response status codes

| Code              | Description                                                  |
| ----------------- | ------------------------------------------------------------ |
| 200 (OK)          | Extended query tag with requested tagPath is successfully returned. |
| 400 (Bad Request) | Requested tag path is invalid.                               |
| 404 (Not Found)   | Extended query tag with requested tagPath is not found       |

### Remove an extended query tag

Remove support for a particular extended query tag using route: /extendedquerytags/{tagPath}

#### Parameter

tagPath is the path for the tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020.

#### Response status codes

| Code              | Description                                                  |
| ----------------- | ------------------------------------------------------------ |
| 204 (No Content)  | Extended query tag with requested tagPath has been successfully deleted. |
| 400 (Bad Request) | Requested tag path is invalid.                               |
| 404 (Not Found)   | Extended query tag with requested tagPath is not found       |

## Integration with DICOMweb™

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



<h3>Definitions</h3>

<h4>Operation</h4>

The long time running operation.

| Name | Type   | Description          |
| ---- | ------ | -------------------- |
| Id   | string | operation id         |
| Href | string | Uri to the operation |

