# Change Feed

The change feed offers the ability to go through the history of the DICOM server and act upon the creates and deletes in the service.

## API Design

The API exposes two `GET` endpoints for interacting with the change feed. A typical flow for consuming the change feed is [provided below](#Example_usage_flow).

Verb | Route              | Returns     | Description
:--- | :----------------- | :---------- | :---
GET  | /changefeed        | Json Array  | [Read the change feed](#Read_change_feed)
GET  | /changefeed/latest | Json Object | [Read the latest entry in the change feed](#Get_latest_change_feed_item)

### Object model
Field               | Type      | Description
:------------------ | :-------- | :---
Sequence            | int       | The order which the operation was executed
StudyInstanceUid    | string    | The study instance UID
SeriesInstanceUid   | string    | The series instance UID
SopInstanceUid      | string    | The sop instance UID
Action              | string    | The action that was performed - either `create` or `delete`
Timestamp           | datetime  | The date and time the action was performed in UTC
State               | string    | [The current state of the metadata](#States)
Metadata            | object    | Optionally, the current DICOM metadata if the instance exists

#### States
State    | Description 
:------- | :--- 
current  | This instance is the current version.
replaced | This instance has been replaced by a new version.
deleted  | This instance has been deleted and is no longer available in the service.

### Read change feed
**Route**: /changefeed?offset={int}&limit={int}&includemetadata={**true**|false}
```
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
            "actual": "metadata"
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
            "actual": "metadata"
        }
    }
    ...
]
```

#### Parameters
Name            | Type | Description
:-------------- | :--- | :---
offset          | int  | The number of records to skip before the values to return
limit           | int  | The number of records to return (default: 10, min: 1, max: 100)
includemetadata | bool | Whether or not to include the metadata (default: true)

### Get latest change feed item
**Route**: /changefeed/latest?includemetadata={**true**|false}
```
{
    "Sequence": 2,
    "StudyInstanceUid": "{uid}",
    "SeriesInstanceUid": "{uid}",
    "SopInstanceUid": "{uid}",
    "Action": "create|delete",
    "Timestamp": "2020-03-05T07:13:16.4834Z",
    "State": "current|replaced|deleted",
    "Metadata": {
        "actual": "metadata"
    }
}
```

#### Parameters
Name            | Type | Description
:-------------- | :--- | :---
includemetadata | bool | Whether or not to include the metadata (default: true)

## Example usage flow
Below is the flow for an example application that wants to do additional processing on the instances within the DICOM service.

1. Application starts that wants to monitor the change feed
2. It determines if there's a current state that it should start with
   * If it has a state, it uses the offset (sequence) stored.
   * If it has never started and wants to start from beginning it uses offset=0  
   * If it only wants to process from now, it queries `/changefeed/latest` to obtain the last sequence
3. It queries the change feed with the given offset `/changefeed?offset={offset}`
4. If there are entries  
  4.1 It performs additional processing  
  4.2 It updates it's current state  
  4.3 It starts again at 2 above  
5. If there are no entries it sleeps for a configured amount of time and starts back at 2.

## Data store design
This is the data store design used for the SQL Server implementation of the change feed.

### `ChangeFeed` Table
Column              | Type         | Description
:------------------ | :----------- | :---
Sequence            | bigint       | An incrementing integer for the actions in the db
StudyInstanceUid    | varchar(64)  |
SeriesInstanceUid   | varchar(64)  |
SopInstanceUid      | varchar(64)  |
Action              | tinyint      | An enum representing the action that was taken
Timestamp           | datetime2(7) | The timestamp for when the action was taken
OriginalWatermark   | bigint       | The watermark of the instance in the original action
CurrentWatermark    | bigint       | The watermark of the current version of the instance

### Flows
#### Create Action
1. Insert row into `Instance` table
2. Insert row into `ChangeFeed` table
3. Update any existing rows `CurrentWatermark` column with the new watermark

#### Delete Action
1. Remove row from `InstanceTable`
2. Insert row into `DeletedInstance` table
3. Insert row into `ChangeFeed` table
3. Update any existing rows `CurrentWatermark` column with null
