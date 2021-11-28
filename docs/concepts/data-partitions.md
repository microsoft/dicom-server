# Overview of Data Partitions

Data partitioning is an optional feature that can be enabled for a DICOM service. It implements a light-weight data partition scheme that enables customers to store multiple copies of the same image with the same identifying instance UIDs on a single DICOM service. 

While UIDs **should** be [unique across all contexts](http://dicom.nema.org/dicom/2013/output/chtml/part05/chapter_9.html), it's common practice for DICOM files to be written to portable storage media by a healthcare provider and given to a patient, who then gives the files to another healthcare provider, who then transfers the files into a new DICOM storage system. Thus, multiple copies of one DICOM file commonly exist in isolated DICOM systems. As DICOM functionality moves to the cloud, unifying previously disconnected systems, data partitioning can provide an on-ramp for your existing data stores and workflows.

## Feature Enablement
The data partitions feature can be enabled by setting the configuration key `DicomServer:Features:EnableDataPartitions` to `true` through your local [appsettings.json](../../src/Microsoft.Health.Dicom.Web/appsettings.json) file or host-specific options.

Once enabled, the feature modifies the API surface of the DICOM server, and makes any previous data accessible under the `Microsoft.Default` partition. 

> *The data partition feature **cannot be disabled** if partitions other than `Microsoft.Default` are present - a `DataPartitionsFeatureCannotBeDisabledException` will be thrown at startup.*

## API Changes
All following URIs assume an implicit DICOM service base URI. For example, the base URI of a DICOM server running locally would be `https://localhost:63838/`. Example requests for new and modified APIs can be found in the [data partition Postman collection](../resources/data-partition-feature.postman_collection.json).

### List Partitions
Lists all data partitions.

```http
GET /partitions
```

#### Request Header

| Name         | Required  | Type   | Description                     |
| ------------ | --------- | ------ | ------------------------------- |
| Content-Type | False     | string | `application/json` is supported |

#### Responses

| Name              | Type                          | Description                           |
| ----------------- | ----------------------------- | ------------------------------------- |
| 200 (OK)          | [Partition](#partition)`[]`   | A list of partitions is returned      |
| 204 (No Content)  |                               | No partitions exist                   |
| 400 (Bad Request) |                               | Data partitions feature is disabled   |

### STOW, WADO, QIDO, and Delete
Once partitions are enabled, STOW, WADO, QIDO, and Delete requests **must** include a data partition URI segment after the base URI, of the form `/partitions/{partitionName}`, where `partitionName` is:
 - Up to 64 characters long
 - Composed of any combination of alphanumeric characters, `.`, `-`, and `_`, to allow both DICOM UID and GUID formats, as well as human-readable identifiers

| Action  | Example URI                                                         |
| ------- | ------------------------------------------------------------------- |
| STOW    | `POST /partitions/myPartition-1/studies`                            |
| WADO    | `GET /partitions/myPartition-1/studies/2.25.0000`                   |
| QIDO    | `GET /partitions/myPartition1/studies?StudyInstanceUID=2.25.0000`   |
| Delete  | `DELETE /partitions/myPartition1/studies/2.25.0000`                 |

#### New Responses

| Name              | Message                                                   |
| ----------------- | --------------------------------------------------------- |
| 400 (Bad Request) | Data partitions feature is disabled                       |
| 400 (Bad Request) | PartitionName value is missing in the route segment.      |
| 400 (Bad Request) | Specified PartitionName {PartitionName} does not exist.   |

### Other APIs
All other APIs (including [extended query tags](../how-to-guides/extended-query-tags.md), [operations](../how-to-guides/extended-query-tags.md#get-operation), and [change feed](change-feed.md)) will continue to be accessed at the base URI. 

## Managing Partitions

Currently, the only management operation supported for partitions is an **implicit** creation during STOW requests.
If the partition specified in the URI does not exist, it will be created implicitly and the response will return a retrieve URI
including the partition path. 

## Limitations
 - If partitions other than `Microsoft.Default` are present, the feature cannot be disabled
 - Querying across partitions is not supported
 - Updating and deleting partitions is not supported 

## Definitions

### Partition
A unit of logical isolation and data uniqueness.

| Name          | Type   | Description                                                                      |
| ------------- | ------ | -------------------------------------------------------------------------------- |
| PartitionKey  | int    | System-assigned identifier                                                       |
| PartitionName | string | Client-assigned unique name, up to 64 alphanumeric characters, `.`, `-`, or `_`  |
| CreatedDate   | string | The date and time when the partition was created                                 |
