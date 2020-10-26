# Custom Tag Design
[[_TOC_]]

## Context

Zeiss requests to support additional custom tags, feature link - [Custom query tags on filters](https://microsofthealth.visualstudio.com/Health/_boards/board/t/Medical%20Imaging/Stories/?workitem=76201).

  

Dicom tag is composed of standard tags and private tags. Standard tags is commonly known tags, full list can be found [here](http://dicom.nema.org/medical/dicom/current/output/chtml/part06/chapter_6.html). Private tags are typically just documented by a device manufacturer in the DICOM Conformance Statement for the product adding the private tags, details can be found [here](http://dicom.nema.org/dicom/2013/output/chtml/part05/sect_7.8.html).

  

There are a list of standard tags are required to support in [standard](http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_10.6.html#table_10.6.1-4), we currently support a [subset](https://github.com/microsoft/dicom-server/blob/master/docs/resources/conformance-statement.md#search-qido-rs).
There are 6 type of attribute matching, and we currently support 2 of them: Range Matching, Single Value Matching. Full list can be found [here](http://dicom.nema.org/medical/dicom/current/output/chtml/part04/sect_C.2.2.2.html).
GCP doesn’t support query on custom tags.
No clear definition in standard for QIDO on custom tags.

  

## Open Questions

### Where to save custom tag configuration?  
Custom Tag configuration can be saved in Config File or Database. 
* ConfigFile: Save configuration in a file, which can be saved locally or storage. When application start, read it into memory, process requests based on which.
* Database: Save configuration in database, process requests based on it.
The custom tag configuration should include tag path (e.g:00101002.00100020 ), tag VR(only required for private tags) and tag level (study/series/instance)

| ID|Solution| Pros| Cons| Proposed | Comments|
|--|--|--|--|--|--|
| 1  | Config File | <p>Faster than \#2, since no need to read database to get config</p> <p>Simpler than \#2, since no need to expose API for custom tag modification</p> |	Not able to process in flight, every change requires restarting application to take effect|||
| 2  | Database    | Able to process in flight| <p>Slower than \#1, requires at least 1 more transaction to get tag configuration</p>	<p>Need to have API for custom tag configuration<p> | ✔        | Manually custom tag modification and application restart is not a good customer scenario, while SQL transaction to retrieve custom tag takes small amount of time, my proposed solution is \#2\.  |

### Data Point
1. What is typical config query database transaction cost?
Estimate 50th Percentile should be <20ms, 90th Percentile should be <100 ms.

We don’t have datapoint on how many custom tag in a query, so I refer to API change feed, since it only have 1 SQL select operation, and not return much data.
|K|Name|Result(ms)|
|--|--|--|
|50|50th Percentile| 20|
|90|90th Percentile| 110|
|99|99th Percentile| 260|

I collected request duration for GetChangeFeed from 6/1/2020 to 10/26/2020 (totally 531) on CI resource group and analyzed data as above. Considering CI use a low performance database (General Purpose: Gen5, 2 vCores), and custom tag request very likely has less data than GetChangeFeed, estimated 50th percentile of this transaction should be less than 20ms.
More details can be found here(TODO: add link)

2. Should we migrate existing tags into custom tag?

| ID | Solution | Pros |Cons | Proposed | Comments |
|--|--|--|--|--|--|
| 1 |Migrate  | Simplify indexing logic, reduce long term engineer maintenance cost. since all tags are processed in same way| <p>Not as good performance as #1.</p> <p>Need to migrate existing data into custom tag table<p> <p>Need move storage to save tag path</p>|||
| 2 |Not Migrate | <p>Better performance. Don’t need to find tag path at first</p> <p>Better storage size. Don’t need to save tag path</p><p>Don’t need to migrate existing data into custom tag table</p>|More long term engineer maintenance cost. More complicated indexing logic. Need to consider 3 cases: custom tag + existing tag/ custom tag only/ existing tag only |✔|<p>Ideally we should put frequently access tags into table column while others into custom tag table, unfortunately we don’t have such data.<p>My assumption is what are defined in [standard](http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_10.6.html#sect_10.6.1.2.3) are more frequently used than others. <p>Base on this assumption, #2 is better than #1, since it optimized most requests.|


#### Open question:
Should we allow customer put [required matching attributes](http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_10.6.html#sect_10.6.1.2.3) which are not supported now as custom tag?

|ID| Solution | Pros | Cons| Proposed| Comments|
|--|--|--|--|--|--|
| 1 | Yes |<p>Not block customer to index on them<p>Save engineer time for adding them into table columns| If we decide to put them in table column in the future, need effort on migration| ✔|Don’t think block customer to use them is a good idea.|
|2|No|<p>Don’t need migration effort in the future if we decide to put them in table column| Customer cannot use them now | |

## High Level Design
### Custom Tag Configuration

4 API are exposed for custom tag operations.
| Operations |  Method |Example Uri| Comments |
|--|--|--|--|
| Add custom tag | Post |…/customtags/|Custom tag information is put in request body as JSON|
| Remove custom tag | Delete|…/customtags/{tag}||
| Get custom tag | Get|…/customtags/{tag}|Custom tag is returned as JSON|
| List custom tags | Get|…/customtags/|Custom tag is returned as JSON|


### Low level design
#### CustomTag Metadata table

    CREATE  TABLE dbo.CustomTagMetadata (    
    TagPath VARCHAR(4000) NOT  NULL,    
    TagVR VARCHAR(2) NOT  NULL,    
    TagLevel TINYINT  NOT  NULL, -- 1: Study 2: Series 3: Instance    
    );
#### Data Table
Each datatype + resource level combination has individual table, totally have 5*3 = 15 tables.

 * String on different levels

    CREATE TABLE dbo.CustomTagStudyString (
    
    StudyKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue NVARCHAR(4000) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagSeriesString (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue NVARCHAR(4000) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagInstanceString (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    InstanceKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue NVARCHAR(4000) NOT NULL,
    
    );

 * DECIMAL on different level

    CREATE TABLE dbo.CustomTagStudyInt (
    
    StudyKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue BIGINT NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagSeriesInt (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue BIGINT NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagInstanceInt (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    InstanceKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue BIGINT NOT NULL,
    
    );

* Decimal on different level

    CREATE TABLE dbo.CustomTagStudyDecimal (
    
    StudyKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue DECIMAL(32, 16) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagSeriesDecimal (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue DECIMAL(32, 16) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagInstanceDecimal (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    InstanceKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue DECIMAL(32, 16) NOT NULL,
    
    );

*  Double on different level

    CREATE TABLE dbo.CustomTagStudyDouble (
    
    StudyKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue FLOAT(53) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagSeriesDouble (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue FLOAT(53) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagInstanceDouble (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    InstanceKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue FLOAT(53) NOT NULL,
    
    );

* datetime on different level

    CREATE TABLE dbo.CustomTagStudyDateTime (
    
    StudyKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue DATETIME2(7) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagSeriesDateTime (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue DATETIME2(7) NOT NULL,
    
    );
    
    CREATE TABLE dbo.CustomTagInstanceDateTime (
    
    StudyKey BIGINT NOT NULL,
    
    SeriesKey BIGINT NOT NULL,
    
    InstanceKey BIGINT NOT NULL,
    
    TagPath VARCHAR(4000) NOT NULL,
    
    TagValue DATETIME2(7) NOT NULL,
    
    );
 

### Open question:

1. should we support fuzzymatch now? (require team input)
2. Permission on custom tag configuration API
Not sure if we need this right now, also not sure how to do this, so could we leave this as backlog?

## Indexing Changes for custom tag
### High level design
TODO: The picture

* During the Store, when add indexes to SQL DB, also index private tag and add into SQL DB.
* During the Query, when query instances from SQL DB, also include private tag in SQL query, when filter out required attributes, also include private tags.
* During the Delete, when removing indexes, also remove private tags.

### Low Level Design
Tag path: the path of tags from root to specific element. 
TODO: the picture
The path to red-circled element is 12050010.12051001.12051005
#### Open Questions
1. How is custom tag data saved into SQL DB?
custom tag data could be any data type, it will be saved as mapping below.

|VRCode	|Name  |Type|Range(byte)  |SQL Type|Comments  |
|--|--|--|--|--|--|
AE|Application Entity|string|<=16|NVARCHAR(4000)| 
AS|Age String|String|4|BIGINT| 
AT|Attribute Tag|Uint|4|BIGINT| 
CS|Code String|string|<=16|NVARCHAR(4000)| 
DA|Date|Date(string)|8|DATETIME2(7)| 
DS|Decimal String|Decimal (string)|<=16|DECIMAL(32,16)|  Decimal String is not exactly C# decimal, it allows E like  5E-4 ([reference](https://groups.google.com/g/comp.protocols.dicom/c/j-z_YVbLtUQ?pli=1))|
DT|Date Time|DateTime5|<=26|DATETIME2(7)| 
FL|Floating Point Single|float|4|FLOAT(53)|Still an open question: SQL float(53) is not able to handle equal comparison
FD|Floating Point Double|double|8|FLOAT(53)| Still an open question: SQL float(53) is not able to handle equal comparison
IS|Integer String|Int(string)|<=12|BIGINT| 
LO|Long String|Long(string)|<=64|BIGINT| 
LT|Long Text|String4|<=10240|NVARCHAR(4000)|Should be skipped if >4000 (DB max capacity)
OB|Other Byte String|Excluded3||| Is excluded from metadata 
OD|Other Double String|Excluded3||| Is excluded from metadata 
OF|Other Float String|Excluded3||| Is excluded from metadata 
OW|Other Word String|Excluded3||| Is excluded from metadata 
PN|Person Name|string|<=64|NVARCHAR(4000)| 
SH|Short String|string|<=16|NVARCHAR(4000)| 
SL|Signed Long|slong|4|BIGINT| 
SQ|Sequence of Items|array||| 
SS|Signed Short|Sshort|2|BIGINT| 
ST|Short Text|string|<=1024|NVARCHAR(4000)| 
TM|Time|Datetime5|16|DATETIME2(7)| 
UI|Unique Identifier (UID)|String|64|NVARCHAR(4000)| 
UL|Unsigned Long|Ulong|4|BIGINT| 
UN|Unknown|Excluded3|||Is excluded from metadata now
US|Unsigned Short|Ushort|2|BIGINT| 
UT|Unlimited Text|String4|232-2|NVARCHAR(4000)|Should be skipped if >4000 (DB max capacity)
 

Notes: When handling with datatime should consider TimeZone offset attribute (0008,0201)

3. STOW

Storage Procedure Dbo.AddInstance is updated as below, a user defined table UDTCustomTagList is used to pass through all private tags.
TODO: image

4. QIDO

QueryParser: modified to support parsing attribute id like 00120013.00A10231.

QueryStore: modified to query with private tag.

QueryResponseBulder: modified to support private tag in included fields.
TODO: image

5. Delete
The DeleteInstance Storage Procedure is modified to delete custom tags
TODO: Image

6. Database Migration (TBD)
Adding new table and updating storage procedure requires database migration.

7. Optimization
Cluster/non-cluster Index should be used for query performance improvement

8. Others

FHIR also have similar feature? [https://dev.azure.com/microsofthealth/Health/_git/health-paas-docs/pullrequest/14038?path=%2Fspecs%2FFHIR%2FCustomSearch%2FCustom-Search.md&_a=files](https://dev.azure.com/microsofthealth/Health/_git/health-paas-docs/pullrequest/14038?path=%2Fspecs%2FFHIR%2FCustomSearch%2FCustom-Search.md&_a=files)

Notes: (TODO: FHIR doesn’t support double, it use decimal(18,6), [link](https://github.com/microsoft/fhir-server/blob/270dcdc5c98537797c00c9e4751e690330f631c0/src/Microsoft.Health.Fhir.SqlServer/Features/Schema/Migrations/4.sql#L450) )