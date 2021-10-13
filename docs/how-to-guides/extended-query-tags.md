# Extended Query Tags

## Overview

Extended query tags allows querying over DICOM tags that are not supported by the DICOMweb™ standard for [QIDO-RS](../resources/conformance-statement.md#search-qido-rs). By enabling this feature, it is possible to query against tags supported by QIDO-RS, publicly defined or "standard" DICOM tags that are not natively supported and private tags.

## Prerequisites

To use this feature, a [Medical Imaging Server for DICOM is required](../quickstarts/deploy-via-azure.md).

### Settings

The current support for the extended query tags feature is exposed in configuration in the following way:

```
{
    "DicomServer": {
        "Features": {
            "EnableExtendedQueryTags": false
        }
}
```

The "EnableExtendedQueryTags" element can be set to true to enable use of this feature. Currently, it is false by default.

## Management APIs

To help manage the supported tags in a given DICOM server instance, a few management APIs are available.

| Verb   | Route                        | Returns     | Description                                                  |
| ------ | ---------------------------- | ----------- | ------------------------------------------------------------ |
| POST   | /extendedquerytags           | Json Object | [Add extended query tag(s) to supported set](#add-extended-query-tags) |
| GET    | /extendedquerytags           | Json Array  | [List all supported extended query tags](#list-all-supported-extended-query-tags) |
| GET    | /extendedquerytags/{tagPath} | Json Object | [Detail an extended query tag's metadata](#get-an-extended-query-tag) |
| DELETE | /extendedquerytags/{tagPath} |             | [Remove support for specified extended query tag](#remove-an-extended-query-tag) |

### Object Model

| Field          | Type   | Description                                                  |
| -------------- | ------ | ------------------------------------------------------------ |
| Path           | string | Path of tag, normally composed of group id and element id. E.g. PatientId (0010,0020) has path 00100020. |
| VR             | string | Value representation of this tag.                            |
| PrivateCreator | string | Identification code of the implementer of this private tag. Only set when the tag is a private tag. |
| Level          | string | Represents the hierarchy at which this tag is relevant.      |
| Status         | string | Current state this tag is in. Not set when making a request to create a tag. |

### Level

| Level    | Description                                      |
| -------- | ------------------------------------------------ |
| Instance | Tag is relevant at the instance level of detail. |
| Series   | Tag is relevant at the series level of detail.   |
| Study    | Tag is relevant at the study level of detail.    |

### Status

| Status   | Description                                      |
| -------- | ------------------------------------------------ |
| Adding   | The tag is being added to the supported set.     |
| Ready    | The tag has been added to the supported set.     |
| Deleting | The tag is being removed from the supported set. |

### Add extended query tags 

Add extended query tags to supported set with route:  /extendedquerytags

`Content-Type` of `application/json` is supported.

#### Request Body

```
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

#### Response

```
{}
```

#### Response status codes

| Code              | Description                                            |
| ----------------- | ------------------------------------------------------ |
| 202 (Accepted)    | Extended query tag(s) have been successfully stored.   |
| 400 (Bad Request) | Requested extended query path has invalid data.        |
| 409 (Conflict)    | One or more requested query tags already is supported. |

### List all supported extended query tags

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
| Range Query | Date (DA), Date Time (DT), Time (TM)       | {attributeID}={value1}-{value2}. For date/ time values, we supported an inclusive range on the tag. This will be mapped to `attributeID >= {value1} AND attributeID <= {value2}`. |
| Exact Match | All             | {attributeID}={value1}                                       |
| Fuzzy Match | PersonName (PN) | Matches any component of the patient name which starts with the value. |

## Limitations

Currently, only the following VR types are supported:

- Application Entity (AE)
- Age String (AS)
- Code String (CS)
- Date (DA)
- Date Time (DT)
- Decimal String (DS)
- Floating Point Double (FD)
- Floating Point Single (FL)
- Integer String (IS)
- Long String (LO)
- Person Name (PN)
- Short String (SH)
- Signed Long (SL)
- Signed Short (SS)
- Time (TM)
- Unique Identifier [UID] (UI)
- Unsigned Long (UL)
- Unsigned Short (US)

Sequential tags i.e. tags under a tag of type Sequence of Items (SQ) are currently not supported. Passing in offsets when querying on DT type is also not supported.

All management APIs are currently synchronous. This means that a [delete](#remove-an-extended-query-tag) request may run long as it attempts to remove any infrastructure that was put in place to support querying against the extended query tag. 

Querying against instances that were stored prior to an extended query tag being added is also not supported. Historical instances would need to be deleted and re-added to enable searching against new extended query tags.

For optimal performance, it is not recommended to store more than 100 extended query tags.

## Summary

In this resource, we reviewed extended query tags, how to use them and how they can enable searching on a wider pool of DICOM tags. 

- To get started with the Medical Imaging Server for DICOM, [Deploy to Azure](../quickstarts/deploy-via-azure.md).