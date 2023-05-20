# Change Feed Overview

The Change Feed provides logs of all the changes that occur in your Medical Imaging Server for DICOM. The Change Feed provides ordered, guaranteed, immutable, read-only log of these changes. The Change Feed offers the ability to go through the history of the Medical Imaging Server for DICOM and act upon the creates and deletes in the service.

Client applications can read these logs at any time in batches of any size. The Change Feed enables you to build efficient and scalable solutions that process change events that occur in your Medical Imaging Server for DICOM.

You can process these change events asynchronously, incrementally, or in-full. Any number of client applications can independently read the Change Feed, in parallel, and at their own pace.

As of v2 of the API, the Change Feed can be queried for a particular time window.

## API Design

The API exposes two `GET` endpoints for interacting with the Change Feed. A typical flow for consuming the Change Feed is [provided below](#example-usage-flow).

Verb | Route              | Returns     | Description
:--- | :----------------- | :---------- | :---
GET  | /changefeed        | Json Array  | [Read the Change Feed](#read-change-feed)
GET  | /changefeed/latest | Json Object | [Read the latest entry in the Change Feed](#get-latest-change-feed-item)

### Object model

Field               | Type      | Description
:------------------ | :-------- | :---
Sequence            | int       | The unique ID per change events
StudyInstanceUid    | string    | The study instance UID
SeriesInstanceUid   | string    | The series instance UID
SopInstanceUid      | string    | The sop instance UID
Action              | string    | The action that was performed - either `create` or `delete`
Timestamp           | datetime  | The date and time the action was performed in UTC
State               | string    | [The current state of the metadata](#states)
Metadata            | object    | Optionally, the current DICOM metadata if the instance exists

#### States

State    | Description
:------- | :---
current  | This instance is the current version.
replaced | This instance has been replaced by a new version.
deleted  | This instance has been deleted and is no longer available in the service.

## Change Feed
The Change Feed resource is a collection of events that have occurred within the DICOM server.

### Version 2

#### Request
```http
GET /changefeed?startTime={datetime}&endtime={datetime}&offset={int}&limit={int}&includemetadata={bool} HTTP/1.1
Accept: */*
Content-Type: application/json
```

#### Response
```json
[
    {
        "Sequence": 1,
        "StudyInstanceUid": "{uid}",
        "SeriesInstanceUid": "{uid}",
        "SopInstanceUid": "{uid}",
        "Action": "create|delete",
        "Timestamp": "2020-03-04T01:03:08.4834Z",
        "State": "current|replaced|deleted",
        "Metadata": {
            // DICOM JSON
        }
    },
    {
        "Sequence": 2,
        "StudyInstanceUid": "{uid}",
        "SeriesInstanceUid": "{uid}",
        "SopInstanceUid": "{uid}",
        "Action": "create|delete",
        "Timestamp": "2020-03-05T07:13:16.4834Z",
        "State": "current|replaced|deleted",
        "Metadata": {
            // DICOM JSON
        }
    },
    // ...
]
```

#### Parameters

Name            | Type     | Description | Default | Min | Max |
:-------------- | :------- | :---------- | :------ | :-- | :-- |
offset          | int      | The number of events to skip from the beginning of the result set | `0` | `0` | |
limit           | int      | The number of records to return | `100` | `1` | `200` |
startTime       | DateTime | The inclusive start time for change events | `"0001-01-01T00:00:00Z"` | `"0001-01-01T00:00:00Z"` | `"9999-12-31T23:59:59.9999998Z"`|
endTime         | DateTime |  The exclusive end time for change events | `"9999-12-31T23:59:59.9999999Z"` | `"0001-01-01T00:00:00.0000001"` | `"9999-12-31T23:59:59.9999999Z"` |
includeMetadata | bool     | Indicates whether or not to include the DICOM metadata | `true` | | |

### Version 1

#### Request
```http
GET /changefeed?offset={int}&limit={int}&includemetadata={bool} HTTP/1.1
Accept: */*
Content-Type: application/json
```

#### Response
```json
[
    {
        "Sequence": 1,
        "StudyInstanceUid": "{uid}",
        "SeriesInstanceUid": "{uid}",
        "SopInstanceUid": "{uid}",
        "Action": "create|delete",
        "Timestamp": "2020-03-04T01:03:08.4834Z",
        "State": "current|replaced|deleted",
        "Metadata": {
            // DICOM JSON
        }
    },
    {
        "Sequence": 2,
        "StudyInstanceUid": "{uid}",
        "SeriesInstanceUid": "{uid}",
        "SopInstanceUid": "{uid}",
        "Action": "create|delete",
        "Timestamp": "2020-03-05T07:13:16.4834Z",
        "State": "current|replaced|deleted",
        "Metadata": {
            // DICOM JSON
        }
    },
    // ...
]
```

#### Parameters
Name            | Type     | Description | Default | Min | Max |
:-------------- | :------- | :---------- | :------ | :-- | :-- |
offset          | int      | The exclusive starting sequence number for events | `0` | `0` | |
limit           | int      | The maximum number of records to return. There may be more results, even if the returned number is less than the limit | `10` | `1` | `100` |
includeMetadata | bool     | Indicates whether or not to include the DICOM metadata | `true` | | |

## Latest Change Feed
The latest Change Feed resource represents the latest event that has occurred within the DICOM Server.

### Request
```http
GET /changefeed/latest?includemetadata={bool} HTTP/1.1
Accept: */*
Content-Type: application/json
```

### Response
```json
{
    "Sequence": 2,
    "StudyInstanceUid": "{uid}",
    "SeriesInstanceUid": "{uid}",
    "SopInstanceUid": "{uid}",
    "Action": "create|delete",
    "Timestamp": "2020-03-05T07:13:16.4834Z",
    "State": "current|replaced|deleted",
    "Metadata": {
        // DICOM JSON
    }
}
```

### Parameters

Name            | Type | Description | Default |
:-------------- | :--- | :---------- | :------ |
includeMetadata | bool | Indicates whether or not to include the metadata | `true` |

## Usage

### DICOM Cast

[DICOM Cast](/converter/dicom-cast) is a stateful processor that pulls DICOM changes from Change Feed, transforms and publishes them to a configured Azure API for FHIR service as an [ImagingStudy resource](https://www.hl7.org/fhir/imagingstudy.html). DICOM Cast can start processing the DICOM change events at any point and continue to pull and process new changes incrementally.

### User Application

Below is the flow for an example application that wants to do additional processing on the instances within the DICOM service.

#### Version 2

1. On some interval, an application queries the Change Feed for the changes within a time range
    * For example, if querying every hour, a query for the Change Feed may look like `/changefeed?startTime=2023-05-10T16:00:00Z&endTime=2023-05-10T17:00:00Z`
    * If starting from the beginning, the Change Feed query may omit the `startTime` to read all of the changes up to, but excluding, the `endTime`
        * E.g. `/changefeed?endTime=2023-05-10T17:00:00Z`
2. Based on the `limit` (if provided), an application continues to query for additional pages of change events if the number of returned events is equal to the `limit` (or default) by updating the offset
    * For example, if the `limit` is `100`, and 100 events are returned, then the subsequent query would include `offset=100` to fetch the next "page" of results. The below queries demonstrate the pattern:
        * `/changefeed?offset=0&limit=100&startTime=2023-05-10T16:00:00Z&endTime=2023-05-10T17:00:00Z`
        * `/changefeed?offset=100&limit=100&startTime=2023-05-10T16:00:00Z&endTime=2023-05-10T17:00:00Z`
        * `/changefeed?offset=200&limit=100&startTime=2023-05-10T16:00:00Z&endTime=2023-05-10T17:00:00Z`
    * If fewer events than the `limit` are returned, then the application can assume that there are no more results

#### Version 1

1. Application that wants to monitor the Change Feed starts.
2. It determines if there's a current state that it should start with:
   * If it has a state, it uses the offset (sequence) stored.
   * If it has never started and wants to start from beginning it uses offset=0
   * If it only wants to process from now, it queries `/changefeed/latest` to obtain the last sequence
3. It queries the Change Feed with the given offset `/changefeed?offset={offset}`
4. If there are entries:
   * It performs additional processing
   * It updates it's current state
   * It starts again at 2 above
5. If there are no entries it sleeps for a configured amount of time and starts back at 2.

### Other potential usage patterns

Change Feed support is well-suited for scenarios that process data based on objects that have changed. For example, it can be used to:

* Build connected application pipelines like ML that react to change events or schedule executions based on created or deleted instance.
* Extract business analytics insights and metrics, based on changes that occur to your objects.
* Poll the Change Feed to create an event source for push notifications.

## Summary

In this Concept, we reviewed the REST API design of Change Feed and potential usage scenarios. For a how-to guide on Change Feed, see [Pull changes from Change Feed](../how-to-guides/pull-changes-from-change-feed.md).
