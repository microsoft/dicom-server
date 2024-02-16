
/*************************************************************************************************
    Auto-Generated from Sql build task. Do not manually edit it. 
**************************************************************************************************/
SET XACT_ABORT ON
BEGIN TRAN
IF EXISTS (SELECT *
           FROM   sys.tables
           WHERE  name = 'Instance')
    BEGIN
        ROLLBACK;
        RETURN;
    END

CREATE SEQUENCE dbo.WatermarkSequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000;

CREATE SEQUENCE dbo.StudyKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000;

CREATE SEQUENCE dbo.SeriesKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000;

CREATE SEQUENCE dbo.InstanceKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000;

CREATE SEQUENCE dbo.TagKeySequence
    AS INT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000;

CREATE SEQUENCE dbo.PartitionKeySequence
    AS INT
    START WITH 2
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000;

CREATE SEQUENCE dbo.WorkitemKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000;

CREATE SEQUENCE dbo.WorkitemWatermarkSequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000;

CREATE TABLE dbo.ChangeFeed (
    Sequence          BIGINT             IDENTITY (1, 1) NOT NULL,
    Timestamp         DATETIMEOFFSET (7) DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    Action            TINYINT            NOT NULL,
    StudyInstanceUid  VARCHAR (64)       NOT NULL,
    SeriesInstanceUid VARCHAR (64)       NOT NULL,
    SopInstanceUid    VARCHAR (64)       NOT NULL,
    OriginalWatermark BIGINT             NOT NULL,
    CurrentWatermark  BIGINT             NULL,
    PartitionKey      INT                DEFAULT 1 NOT NULL,
    FilePath          NVARCHAR (4000)    NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_ChangeFeed
    ON dbo.ChangeFeed(Timestamp, Sequence);

CREATE NONCLUSTERED INDEX IX_ChangeFeed_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid
    ON dbo.ChangeFeed(PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_ChangeFeed_Sequence
    ON dbo.ChangeFeed(Sequence) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.DeletedInstance (
    StudyInstanceUid  VARCHAR (64)       NOT NULL,
    SeriesInstanceUid VARCHAR (64)       NOT NULL,
    SopInstanceUid    VARCHAR (64)       NOT NULL,
    Watermark         BIGINT             NOT NULL,
    DeletedDateTime   DATETIMEOFFSET (0) NOT NULL,
    RetryCount        INT                NOT NULL,
    CleanupAfter      DATETIMEOFFSET (0) NOT NULL,
    PartitionKey      INT                DEFAULT 1 NOT NULL,
    OriginalWatermark BIGINT             NULL,
    FilePath          NVARCHAR (4000)    NULL,
    ETag              NVARCHAR (4000)    NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_DeletedInstance
    ON dbo.DeletedInstance(PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark);

CREATE NONCLUSTERED INDEX IX_DeletedInstance_RetryCount_CleanupAfter
    ON dbo.DeletedInstance(RetryCount, CleanupAfter)
    INCLUDE(PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, OriginalWatermark, FilePath, ETag) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.ExtendedQueryTag (
    TagKey            INT           NOT NULL,
    TagPath           VARCHAR (64)  NOT NULL,
    TagVR             VARCHAR (2)   NOT NULL,
    TagPrivateCreator NVARCHAR (64) NULL,
    TagLevel          TINYINT       NOT NULL,
    TagStatus         TINYINT       NOT NULL,
    QueryStatus       TINYINT       DEFAULT 1 NOT NULL,
    ErrorCount        INT           DEFAULT 0 NOT NULL
);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTag
    ON dbo.ExtendedQueryTag(TagKey);

CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTag_TagPath
    ON dbo.ExtendedQueryTag(TagPath);

CREATE TABLE dbo.ExtendedQueryTagDateTime (
    TagKey          INT           NOT NULL,
    TagValue        DATETIME2 (7) NOT NULL,
    SopInstanceKey1 BIGINT        NOT NULL,
    SopInstanceKey2 BIGINT        NULL,
    SopInstanceKey3 BIGINT        NULL,
    Watermark       BIGINT        NOT NULL,
    TagValueUtc     DATETIME2 (7) NULL,
    PartitionKey    INT           DEFAULT 1 NOT NULL,
    ResourceType    TINYINT       DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime
    ON dbo.ExtendedQueryTagDateTime(PartitionKey, ResourceType, TagKey, TagValue, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_PartitionKey_TagKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagDateTime(PartitionKey, ResourceType, TagKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3)
    INCLUDE(Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagDateTime(PartitionKey, ResourceType, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.ExtendedQueryTagDouble (
    TagKey          INT        NOT NULL,
    TagValue        FLOAT (53) NOT NULL,
    SopInstanceKey1 BIGINT     NOT NULL,
    SopInstanceKey2 BIGINT     NULL,
    SopInstanceKey3 BIGINT     NULL,
    Watermark       BIGINT     NOT NULL,
    PartitionKey    INT        DEFAULT 1 NOT NULL,
    ResourceType    TINYINT    DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDouble
    ON dbo.ExtendedQueryTagDouble(PartitionKey, ResourceType, TagKey, TagValue, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_PartitionKey_TagKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagDouble(PartitionKey, ResourceType, TagKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3)
    INCLUDE(Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagDouble(PartitionKey, ResourceType, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.ExtendedQueryTagError (
    TagKey      INT           NOT NULL,
    ErrorCode   SMALLINT      NOT NULL,
    Watermark   BIGINT        NOT NULL,
    CreatedTime DATETIME2 (7) NOT NULL
);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagError
    ON dbo.ExtendedQueryTagError(TagKey, Watermark);

CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagError_CreatedTime_Watermark_TagKey
    ON dbo.ExtendedQueryTagError(CreatedTime, Watermark, TagKey)
    INCLUDE(ErrorCode);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagError_Watermark
    ON dbo.ExtendedQueryTagError(Watermark);

CREATE TABLE dbo.ExtendedQueryTagLong (
    TagKey          INT     NOT NULL,
    TagValue        BIGINT  NOT NULL,
    SopInstanceKey1 BIGINT  NOT NULL,
    SopInstanceKey2 BIGINT  NULL,
    SopInstanceKey3 BIGINT  NULL,
    Watermark       BIGINT  NOT NULL,
    PartitionKey    INT     DEFAULT 1 NOT NULL,
    ResourceType    TINYINT DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagLong
    ON dbo.ExtendedQueryTagLong(PartitionKey, ResourceType, TagKey, TagValue, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_PartitionKey_TagKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagLong(PartitionKey, ResourceType, TagKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3)
    INCLUDE(Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagLong(PartitionKey, ResourceType, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.ExtendedQueryTagOperation (
    TagKey      INT              NOT NULL,
    OperationId UNIQUEIDENTIFIER NOT NULL
);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagOperation
    ON dbo.ExtendedQueryTagOperation(TagKey);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagOperation_OperationId
    ON dbo.ExtendedQueryTagOperation(OperationId)
    INCLUDE(TagKey);

CREATE TABLE dbo.ExtendedQueryTagPersonName (
    TagKey             INT            NOT NULL,
    TagValue           NVARCHAR (200) COLLATE SQL_Latin1_General_CP1_CI_AI NOT NULL,
    SopInstanceKey1    BIGINT         NOT NULL,
    SopInstanceKey2    BIGINT         NULL,
    SopInstanceKey3    BIGINT         NULL,
    Watermark          BIGINT         NOT NULL,
    WatermarkAndTagKey AS             CONCAT(TagKey, '.', Watermark),
    TagValueWords      AS             REPLACE(REPLACE(TagValue, '^', ' '), '=', ' ') PERSISTED,
    PartitionKey       INT            DEFAULT 1 NOT NULL,
    ResourceType       TINYINT        DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName
    ON dbo.ExtendedQueryTagPersonName(PartitionKey, ResourceType, TagKey, TagValue, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagPersonName(PartitionKey, ResourceType, TagKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3)
    INCLUDE(Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagPersonName(PartitionKey, ResourceType, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey
    ON dbo.ExtendedQueryTagPersonName(WatermarkAndTagKey) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.ExtendedQueryTagString (
    TagKey          INT           NOT NULL,
    TagValue        NVARCHAR (64) NOT NULL,
    SopInstanceKey1 BIGINT        NOT NULL,
    SopInstanceKey2 BIGINT        NULL,
    SopInstanceKey3 BIGINT        NULL,
    Watermark       BIGINT        NOT NULL,
    PartitionKey    INT           DEFAULT 1 NOT NULL,
    ResourceType    TINYINT       DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagString
    ON dbo.ExtendedQueryTagString(PartitionKey, ResourceType, TagKey, TagValue, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_PartitionKey_TagKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagString(PartitionKey, ResourceType, TagKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3)
    INCLUDE(Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3
    ON dbo.ExtendedQueryTagString(PartitionKey, ResourceType, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.FileProperty (
    InstanceKey   BIGINT          NOT NULL,
    Watermark     BIGINT          NOT NULL,
    FilePath      NVARCHAR (4000) NOT NULL,
    ETag          NVARCHAR (4000) NOT NULL,
    ContentLength BIGINT          DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_FileProperty
    ON dbo.FileProperty(InstanceKey, Watermark) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON);

CREATE NONCLUSTERED INDEX IXC_FileProperty_InstanceKey_Watermark_ContentLength
    ON dbo.FileProperty(InstanceKey, Watermark, ContentLength) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON);

CREATE NONCLUSTERED INDEX IXC_FileProperty_ContentLength
    ON dbo.FileProperty(ContentLength) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON);

CREATE TABLE dbo.Instance (
    InstanceKey           BIGINT        NOT NULL,
    SeriesKey             BIGINT        NOT NULL,
    StudyKey              BIGINT        NOT NULL,
    StudyInstanceUid      VARCHAR (64)  NOT NULL,
    SeriesInstanceUid     VARCHAR (64)  NOT NULL,
    SopInstanceUid        VARCHAR (64)  NOT NULL,
    Watermark             BIGINT        NOT NULL,
    Status                TINYINT       NOT NULL,
    LastStatusUpdatedDate DATETIME2 (7) NOT NULL,
    CreatedDate           DATETIME2 (7) NOT NULL,
    PartitionKey          INT           DEFAULT 1 NOT NULL,
    TransferSyntaxUid     VARCHAR (64)  NULL,
    HasFrameMetadata      BIT           DEFAULT 0 NOT NULL,
    OriginalWatermark     BIGINT        NULL,
    NewWatermark          BIGINT        NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_Instance
    ON dbo.Instance(PartitionKey, StudyKey, SeriesKey, InstanceKey);

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid
    ON dbo.Instance(PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid)
    INCLUDE(Status, Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid
    ON dbo.Instance(PartitionKey, Status, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid)
    INCLUDE(Watermark, TransferSyntaxUid, HasFrameMetadata) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_Watermark_Status
    ON dbo.Instance(Watermark, Status)
    INCLUDE(PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_SopInstanceUid
    ON dbo.Instance(PartitionKey, SopInstanceUid)
    INCLUDE(SeriesKey) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyKey_Watermark
    ON dbo.Instance(PartitionKey, Status, StudyKey, Watermark)
    INCLUDE(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyKey_SeriesKey_Watermark
    ON dbo.Instance(PartitionKey, Status, StudyKey, SeriesKey, Watermark)
    INCLUDE(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Watermark
    ON dbo.Instance(PartitionKey, Watermark)
    INCLUDE(StudyKey, SeriesKey, StudyInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyInstanceUid_NewWatermark
    ON dbo.Instance(PartitionKey, Status, StudyInstanceUid, NewWatermark)
    INCLUDE(SeriesInstanceUid, SopInstanceUid, Watermark, OriginalWatermark) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Instance_Watermark_Status_CreatedDate
    ON dbo.Instance(Watermark, Status, CreatedDate)
    INCLUDE(PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.Partition (
    PartitionKey  INT           NOT NULL,
    PartitionName VARCHAR (64)  NOT NULL,
    CreatedDate   DATETIME2 (7) NOT NULL
);

CREATE UNIQUE CLUSTERED INDEX IXC_Partition
    ON dbo.Partition(PartitionKey);

CREATE UNIQUE NONCLUSTERED INDEX IX_Partition_PartitionName
    ON dbo.Partition(PartitionName)
    INCLUDE(PartitionKey);

INSERT  INTO dbo.Partition (PartitionKey, PartitionName, CreatedDate)
VALUES                    (1, 'Microsoft.Default', SYSUTCDATETIME());

CREATE TABLE dbo.Series (
    SeriesKey                       BIGINT        NOT NULL,
    StudyKey                        BIGINT        NOT NULL,
    SeriesInstanceUid               VARCHAR (64)  NOT NULL,
    Modality                        NVARCHAR (16) NULL,
    PerformedProcedureStepStartDate DATE          NULL,
    ManufacturerModelName           NVARCHAR (64) NULL,
    PartitionKey                    INT           DEFAULT 1 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_Series
    ON dbo.Series(PartitionKey, StudyKey, SeriesKey);

CREATE UNIQUE NONCLUSTERED INDEX IX_Series_PartitionKey_StudyKey_SeriesInstanceUid
    ON dbo.Series(PartitionKey, StudyKey, SeriesInstanceUid) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_SeriesInstanceUid
    ON dbo.Series(PartitionKey, SeriesInstanceUid)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_Modality
    ON dbo.Series(PartitionKey, Modality)
    INCLUDE(StudyKey, SeriesKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_PerformedProcedureStepStartDate
    ON dbo.Series(PartitionKey, PerformedProcedureStepStartDate)
    INCLUDE(StudyKey, SeriesKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_ManufacturerModelName
    ON dbo.Series(PartitionKey, ManufacturerModelName)
    INCLUDE(StudyKey, SeriesKey) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.Study (
    StudyKey                    BIGINT         NOT NULL,
    StudyInstanceUid            VARCHAR (64)   NOT NULL,
    PatientId                   NVARCHAR (64)  NOT NULL,
    PatientName                 NVARCHAR (200) COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    ReferringPhysicianName      NVARCHAR (200) COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    StudyDate                   DATE           NULL,
    StudyDescription            NVARCHAR (64)  NULL,
    AccessionNumber             NVARCHAR (16)  NULL,
    PatientNameWords            AS             REPLACE(REPLACE(PatientName, '^', ' '), '=', ' ') PERSISTED,
    ReferringPhysicianNameWords AS             REPLACE(REPLACE(ReferringPhysicianName, '^', ' '), '=', ' ') PERSISTED,
    PatientBirthDate            DATE           NULL,
    PartitionKey                INT            DEFAULT 1 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_Study
    ON dbo.Study(PartitionKey, StudyKey);

CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyKey
    ON dbo.Study(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyInstanceUid
    ON dbo.Study(PartitionKey, StudyInstanceUid)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_PatientId
    ON dbo.Study(PartitionKey, PatientId)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_PatientName
    ON dbo.Study(PartitionKey, PatientName)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_ReferringPhysicianName
    ON dbo.Study(PartitionKey, ReferringPhysicianName)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyDate
    ON dbo.Study(PartitionKey, StudyDate)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyDescription
    ON dbo.Study(PartitionKey, StudyDescription)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_AccessionNumber
    ON dbo.Study(PartitionKey, AccessionNumber)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_PatientBirthDate
    ON dbo.Study(PartitionKey, PatientBirthDate)
    INCLUDE(StudyKey) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.Workitem (
    WorkitemKey           BIGINT        NOT NULL,
    PartitionKey          INT           DEFAULT 1 NOT NULL,
    WorkitemUid           VARCHAR (64)  NOT NULL,
    TransactionUid        VARCHAR (64)  NULL,
    Status                TINYINT       NOT NULL,
    CreatedDate           DATETIME2 (7) NOT NULL,
    LastStatusUpdatedDate DATETIME2 (7) NOT NULL,
    Watermark             BIGINT        DEFAULT 0 NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_Workitem
    ON dbo.Workitem(PartitionKey, WorkitemKey);

CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_PartitionKey_WorkitemUid
    ON dbo.Workitem(PartitionKey, WorkitemUid)
    INCLUDE(Watermark, WorkitemKey, Status, TransactionUid) WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemKey_Watermark
    ON dbo.Workitem(WorkitemKey, Watermark) WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.WorkitemQueryTag (
    TagKey  INT          NOT NULL,
    TagPath VARCHAR (64) NOT NULL,
    TagVR   VARCHAR (2)  NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE UNIQUE CLUSTERED INDEX IXC_WorkitemQueryTag
    ON dbo.WorkitemQueryTag(TagKey);

CREATE UNIQUE NONCLUSTERED INDEX IXC_WorkitemQueryTag_TagPath
    ON dbo.WorkitemQueryTag(TagPath) WITH (DATA_COMPRESSION = PAGE);

CREATE TYPE dbo.AddExtendedQueryTagsInputTableType_1 AS TABLE (
    TagPath           VARCHAR (64) ,
    TagVR             VARCHAR (2)  ,
    TagPrivateCreator NVARCHAR (64),
    TagLevel          TINYINT      );

CREATE TYPE dbo.InsertStringExtendedQueryTagTableType_1 AS TABLE (
    TagKey   INT          ,
    TagValue NVARCHAR (64),
    TagLevel TINYINT      );

CREATE TYPE dbo.InsertDoubleExtendedQueryTagTableType_1 AS TABLE (
    TagKey   INT       ,
    TagValue FLOAT (53),
    TagLevel TINYINT   );

CREATE TYPE dbo.InsertLongExtendedQueryTagTableType_1 AS TABLE (
    TagKey   INT    ,
    TagValue BIGINT ,
    TagLevel TINYINT);

CREATE TYPE dbo.InsertDateTimeExtendedQueryTagTableType_1 AS TABLE (
    TagKey   INT          ,
    TagValue DATETIME2 (7),
    TagLevel TINYINT      );

CREATE TYPE dbo.InsertDateTimeExtendedQueryTagTableType_2 AS TABLE (
    TagKey      INT          ,
    TagValue    DATETIME2 (7),
    TagValueUtc DATETIME2 (7) NULL,
    TagLevel    TINYINT      );

CREATE TYPE dbo.InsertPersonNameExtendedQueryTagTableType_1 AS TABLE (
    TagKey   INT           ,
    TagValue NVARCHAR (200) COLLATE SQL_Latin1_General_CP1_CI_AI,
    TagLevel TINYINT       );

CREATE TYPE dbo.ExtendedQueryTagKeyTableType_1 AS TABLE (
    TagKey INT);

CREATE TYPE dbo.WatermarkTableType AS TABLE (
    Watermark BIGINT);

CREATE TYPE dbo.FilePropertyTableType AS TABLE (
    Watermark BIGINT          NOT NULL INDEX IXC_FilePropertyTableType CLUSTERED,
    FilePath  NVARCHAR (4000) NOT NULL,
    ETag      NVARCHAR (4000) NOT NULL);

CREATE TYPE dbo.FilePropertyTableType_2 AS TABLE (
    Watermark     BIGINT          NOT NULL INDEX IXC_FilePropertyTableType_2 CLUSTERED,
    FilePath      NVARCHAR (4000) NOT NULL,
    ETag          NVARCHAR (4000) NOT NULL,
    ContentLength BIGINT          NOT NULL);

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00100010', 'PN');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00100020', 'LO');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '0040A370.00080050', 'SQ');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '0040A370.00401001', 'SQ');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00404005', 'DT');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00404025.00080100', 'SQ');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00404026.00080100', 'SQ');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00741000', 'CS');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '00404027.00080100', 'SQ');

INSERT  INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES                           ( NEXT VALUE FOR TagKeySequence, '0020000D', 'UI');

COMMIT
GO
IF ((SELECT is_read_committed_snapshot_on
     FROM   sys.databases
     WHERE  database_id = DB_ID()) = 0)
    BEGIN
        ALTER DATABASE CURRENT
            SET READ_COMMITTED_SNAPSHOT ON;
    END

IF ((SELECT is_auto_update_stats_async_on
     FROM   sys.databases
     WHERE  database_id = DB_ID()) = 0)
    BEGIN
        ALTER DATABASE CURRENT
            SET AUTO_UPDATE_STATISTICS_ASYNC ON;
    END

IF ((SELECT is_ansi_nulls_on
     FROM   sys.databases
     WHERE  database_id = DB_ID()) = 0)
    BEGIN
        ALTER DATABASE CURRENT
            SET ANSI_NULLS ON;
    END

GO
IF NOT EXISTS (SELECT *
               FROM   sys.fulltext_catalogs
               WHERE  name = 'Dicom_Catalog')
    BEGIN
        CREATE FULLTEXT CATALOG Dicom_Catalog
            WITH ACCENT_SENSITIVITY = OFF
            AS DEFAULT;
    END


GO
IF NOT EXISTS (SELECT *
               FROM   sys.fulltext_indexes
               WHERE  object_id = object_id('dbo.Study'))
    BEGIN
        CREATE FULLTEXT INDEX ON Study
            (PatientNameWords, ReferringPhysicianNameWords LANGUAGE 1033)
            KEY INDEX IX_Study_StudyKey
            WITH STOPLIST OFF;
    END


GO
IF NOT EXISTS (SELECT *
               FROM   sys.fulltext_indexes
               WHERE  object_id = object_id('dbo.ExtendedQueryTagPersonName'))
    BEGIN
        CREATE FULLTEXT INDEX ON ExtendedQueryTagPersonName
            (TagValueWords LANGUAGE 1033)
            KEY INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey
            WITH STOPLIST OFF;
    END

GO
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTagError
@tagKey INT, @errorCode SMALLINT, @watermark BIGINT
AS
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
IF NOT EXISTS (SELECT *
               FROM   dbo.Instance WITH (UPDLOCK)
               WHERE  Watermark = @watermark
                      AND Status = 1)
    THROW 50404, 'Instance does not exist or has not been created.', 1;
IF NOT EXISTS (SELECT *
               FROM   dbo.ExtendedQueryTag WITH (UPDLOCK)
               WHERE  TagKey = @tagKey
                      AND TagStatus = 0)
    THROW 50404, 'Tag does not exist or is not being added.', 1;
DECLARE @addedCount AS SMALLINT;
SET @addedCount = 1;
MERGE INTO dbo.ExtendedQueryTagError WITH (HOLDLOCK)
 AS XQTE
USING (SELECT @tagKey AS TagKey,
              @errorCode AS ErrorCode,
              @watermark AS Watermark) AS src ON src.TagKey = XQTE.TagKey
                                                 AND src.WaterMark = XQTE.Watermark
WHEN MATCHED THEN UPDATE 
SET CreatedTime = @currentDate,
    ErrorCode   = @errorCode,
    @addedCount = 0
WHEN NOT MATCHED THEN INSERT (TagKey, ErrorCode, Watermark, CreatedTime) VALUES (@tagKey, @errorCode, @watermark, @currentDate) OUTPUT INSERTED.TagKey;
UPDATE dbo.ExtendedQueryTag
SET    QueryStatus = 0,
       ErrorCount  = ErrorCount + @addedCount
WHERE  TagKey = @tagKey;
COMMIT TRANSACTION;

GO
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTags
@extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY, @maxAllowedCount INT=128, @ready BIT=0
AS
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN
    BEGIN TRANSACTION;
    IF (SELECT COUNT(*)
        FROM   dbo.ExtendedQueryTag AS XQT WITH (UPDLOCK, HOLDLOCK)
               FULL OUTER JOIN
               @extendedQueryTags AS input
               ON XQT.TagPath = input.TagPath) > @maxAllowedCount
        THROW 50409, 'extended query tags exceed max allowed count', 1;
    DECLARE @existingTags TABLE (
        TagKey      INT             ,
        TagStatus   TINYINT         ,
        OperationId UNIQUEIDENTIFIER NULL);
    INSERT INTO @existingTags (TagKey, TagStatus, OperationId)
    SELECT XQT.TagKey,
           TagStatus,
           OperationId
    FROM   dbo.ExtendedQueryTag AS XQT
           INNER JOIN
           @extendedQueryTags AS input
           ON input.TagPath = XQT.TagPath
           LEFT OUTER JOIN
           dbo.ExtendedQueryTagOperation AS XQTO
           ON XQT.TagKey = XQTO.TagKey;
    IF EXISTS (SELECT 1
               FROM   @existingTags
               WHERE  TagStatus <> 0
                      OR (TagStatus = 0
                          AND OperationId IS NOT NULL))
        THROW 50409, 'extended query tag(s) already exist', 2;
    DELETE XQT
    FROM   dbo.ExtendedQueryTag AS XQT
           INNER JOIN
           @existingTags AS et
           ON XQT.TagKey = et.TagKey;
    INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount)
    OUTPUT INSERTED.TagKey, INSERTED.TagPath, INSERTED.TagVR, INSERTED.TagPrivateCreator, INSERTED.TagLevel, INSERTED.TagStatus, INSERTED.QueryStatus, INSERTED.ErrorCount
    SELECT  NEXT VALUE FOR TagKeySequence,
           TagPath,
           TagPrivateCreator,
           TagVR,
           TagLevel,
           @ready,
           1,
           0
    FROM   @extendedQueryTags;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.AddInstanceV6
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @patientId NVARCHAR (64), @patientName NVARCHAR (325)=NULL, @referringPhysicianName NVARCHAR (325)=NULL, @studyDate DATE=NULL, @studyDescription NVARCHAR (64)=NULL, @accessionNumber NVARCHAR (64)=NULL, @modality NVARCHAR (16)=NULL, @performedProcedureStepStartDate DATE=NULL, @patientBirthDate DATE=NULL, @manufacturerModelName NVARCHAR (64)=NULL, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY, @initialStatus TINYINT, @transferSyntaxUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
        DECLARE @existingStatus AS TINYINT;
        DECLARE @newWatermark AS BIGINT;
        DECLARE @studyKey AS BIGINT;
        DECLARE @seriesKey AS BIGINT;
        DECLARE @instanceKey AS BIGINT;
        SELECT @existingStatus = Status
        FROM   dbo.Instance
        WHERE  PartitionKey = @partitionKey
               AND StudyInstanceUid = @studyInstanceUid
               AND SeriesInstanceUid = @seriesInstanceUid
               AND SopInstanceUid = @sopInstanceUid;
        IF @@ROWCOUNT <> 0
            THROW 50409, 'Instance already exists', @existingStatus;
        SET @newWatermark =  NEXT VALUE FOR dbo.WatermarkSequence;
        SET @instanceKey =  NEXT VALUE FOR dbo.InstanceKeySequence;
        SELECT @studyKey = StudyKey
        FROM   dbo.Study WITH (UPDLOCK)
        WHERE  PartitionKey = @partitionKey
               AND StudyInstanceUid = @studyInstanceUid;
        IF @@ROWCOUNT = 0
            BEGIN TRY
                SET @studyKey =  NEXT VALUE FOR dbo.StudyKeySequence;
                INSERT  INTO dbo.Study (PartitionKey, StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
                VALUES                (@partitionKey, @studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber);
            END TRY
            BEGIN CATCH
                IF ERROR_NUMBER() = 2601
                    BEGIN
                        SELECT @studyKey = StudyKey
                        FROM   dbo.Study WITH (UPDLOCK)
                        WHERE  PartitionKey = @partitionKey
                               AND StudyInstanceUid = @studyInstanceUid;
                        UPDATE dbo.Study
                        SET    PatientId              = (CASE WHEN @patientId IS NOT NULL
                                                                   AND @patientId <> '' THEN @patientId ELSE PatientId END),
                               PatientName            = ISNULL(@patientName, PatientName),
                               PatientBirthDate       = ISNULL(@patientBirthDate, PatientBirthDate),
                               ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName),
                               StudyDate              = ISNULL(@studyDate, StudyDate),
                               StudyDescription       = ISNULL(@studyDescription, StudyDescription),
                               AccessionNumber        = ISNULL(@accessionNumber, AccessionNumber)
                        WHERE  PartitionKey = @partitionKey
                               AND StudyKey = @studyKey;
                    END
                ELSE
                    THROW;
            END CATCH
        ELSE
            BEGIN
                UPDATE dbo.Study
                SET    PatientId              = (CASE WHEN @patientId IS NOT NULL
                                                           AND @patientId <> '' THEN @patientId ELSE PatientId END),
                       PatientName            = ISNULL(@patientName, PatientName),
                       PatientBirthDate       = ISNULL(@patientBirthDate, PatientBirthDate),
                       ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName),
                       StudyDate              = ISNULL(@studyDate, StudyDate),
                       StudyDescription       = ISNULL(@studyDescription, StudyDescription),
                       AccessionNumber        = ISNULL(@accessionNumber, AccessionNumber)
                WHERE  PartitionKey = @partitionKey
                       AND StudyKey = @studyKey;
            END
        SELECT @seriesKey = SeriesKey
        FROM   dbo.Series WITH (UPDLOCK)
        WHERE  StudyKey = @studyKey
               AND SeriesInstanceUid = @seriesInstanceUid
               AND PartitionKey = @partitionKey;
        IF @@ROWCOUNT = 0
            BEGIN
                SET @seriesKey =  NEXT VALUE FOR dbo.SeriesKeySequence;
                INSERT  INTO dbo.Series (PartitionKey, StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate, ManufacturerModelName)
                VALUES                 (@partitionKey, @studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate, @manufacturerModelName);
            END
        ELSE
            BEGIN
                UPDATE dbo.Series
                SET    Modality                        = ISNULL(@modality, Modality),
                       PerformedProcedureStepStartDate = ISNULL(@performedProcedureStepStartDate, PerformedProcedureStepStartDate),
                       ManufacturerModelName           = ISNULL(@manufacturerModelName, ManufacturerModelName)
                WHERE  SeriesKey = @seriesKey
                       AND StudyKey = @studyKey
                       AND PartitionKey = @partitionKey;
            END
        INSERT  INTO dbo.Instance (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate, TransferSyntaxUid)
        VALUES                   (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate, @transferSyntaxUid);
        BEGIN TRY
            EXECUTE dbo.IIndexInstanceCoreV9 @partitionKey, @studyKey, @seriesKey, @instanceKey, @newWatermark, @stringExtendedQueryTags, @longExtendedQueryTags, @doubleExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
        END TRY
        BEGIN CATCH
            THROW;
        END CATCH
        SELECT @newWatermark;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        THROW;
    END CATCH
END

GO
CREATE OR ALTER PROCEDURE dbo.AddPartition
@partitionName VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @createdDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @partitionKey AS INT;
    SELECT @partitionKey = PartitionKey
    FROM   dbo.Partition
    WHERE  PartitionName = @partitionName;
    IF @@ROWCOUNT <> 0
        THROW 50409, 'Partition already exists', 1;
    SET @partitionKey =  NEXT VALUE FOR dbo.PartitionKeySequence;
    INSERT  INTO dbo.Partition (PartitionKey, PartitionName, CreatedDate)
    VALUES                    (@partitionKey, @partitionName, @createdDate);
    SELECT @partitionKey,
           @partitionName,
           @createdDate;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.AddWorkitem
@partitionKey INT, @workitemUid VARCHAR (64), @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY, @initialStatus TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @workitemKey AS BIGINT;
    SELECT @workitemKey = WorkitemKey
    FROM   dbo.Workitem
    WHERE  PartitionKey = @partitionKey
           AND WorkitemUid = @workitemUid;
    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', 1;
    SET @workitemKey =  NEXT VALUE FOR dbo.WorkitemKeySequence;
    INSERT  INTO dbo.Workitem (WorkitemKey, PartitionKey, WorkitemUid, Status, CreatedDate, LastStatusUpdatedDate)
    VALUES                   (@workitemKey, @partitionKey, @workitemUid, @initialStatus, @currentDate, @currentDate);
    BEGIN TRY
        EXECUTE dbo.IIndexWorkitemInstanceCore @partitionKey, @workitemKey, @stringExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    SELECT @workitemKey;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.AddWorkitemV11
@partitionKey INT, @workitemUid VARCHAR (64), @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY, @initialStatus TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @watermark AS BIGINT;
    DECLARE @workitemKey AS BIGINT;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    SELECT @workitemKey = WorkitemKey
    FROM   dbo.Workitem
    WHERE  PartitionKey = @partitionKey
           AND WorkitemUid = @workitemUid;
    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', 1;
    SET @workitemKey =  NEXT VALUE FOR dbo.WorkitemKeySequence;
    SET @watermark =  NEXT VALUE FOR dbo.WorkitemWatermarkSequence;
    INSERT  INTO dbo.Workitem (WorkitemKey, PartitionKey, WorkitemUid, Status, Watermark, CreatedDate, LastStatusUpdatedDate)
    VALUES                   (@workitemKey, @partitionKey, @workitemUid, @initialStatus, @watermark, @currentDate, @currentDate);
    BEGIN TRY
        EXECUTE dbo.IIndexWorkitemInstanceCore @partitionKey, @workitemKey, @stringExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    COMMIT TRANSACTION;
    SELECT @workitemKey,
           @watermark;
END

GO
CREATE OR ALTER PROCEDURE dbo.AssignReindexingOperation
@extendedQueryTagKeys dbo.ExtendedQueryTagKeyTableType_1 READONLY, @operationId UNIQUEIDENTIFIER, @returnIfCompleted BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    MERGE INTO dbo.ExtendedQueryTagOperation WITH (HOLDLOCK)
     AS XQTO
    USING (SELECT input.TagKey
           FROM   @extendedQueryTagKeys AS input
                  INNER JOIN
                  dbo.ExtendedQueryTag AS XQT WITH (HOLDLOCK)
                  ON input.TagKey = XQT.TagKey
           WHERE  TagStatus = 0) AS tags ON XQTO.TagKey = tags.TagKey
    WHEN NOT MATCHED THEN INSERT (TagKey, OperationId) VALUES (tags.TagKey, @operationId);
    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount
    FROM   @extendedQueryTagKeys AS input
           INNER JOIN
           dbo.ExtendedQueryTag AS XQT WITH (HOLDLOCK)
           ON input.TagKey = XQT.TagKey
           LEFT OUTER JOIN
           dbo.ExtendedQueryTagOperation AS XQTO WITH (HOLDLOCK)
           ON XQT.TagKey = XQTO.TagKey
    WHERE  (@returnIfCompleted = 1
            AND TagStatus = 1)
           OR (OperationId = @operationId
               AND TagStatus = 0);
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstance
@partitionKey INT, @watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE i
    SET    NewWatermark =  NEXT VALUE FOR dbo.WatermarkSequence
    FROM   dbo.Instance AS i
           INNER JOIN
           @watermarkTableType AS input
           ON i.Watermark = input.Watermark
              AND i.PartitionKey = @partitionKey
    WHERE  Status = 1;
    COMMIT TRANSACTION;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           i.Watermark,
           TransferSyntaxUid,
           HasFrameMetadata,
           OriginalWatermark,
           NewWatermark
    FROM   dbo.Instance AS i
           INNER JOIN
           @watermarkTableType AS input
           ON i.Watermark = input.Watermark
              AND i.PartitionKey = @partitionKey
    WHERE  Status = 1;
END

GO
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstanceV33
@partitionKey INT, @studyInstanceUid VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.Instance
    SET    NewWatermark =  NEXT VALUE FOR dbo.WatermarkSequence
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1;
    COMMIT TRANSACTION;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark,
           TransferSyntaxUid,
           HasFrameMetadata,
           OriginalWatermark,
           NewWatermark
    FROM   dbo.Instance
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1;
END

GO
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstanceV46
@partitionKey INT, @studyInstanceUid VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.Instance
    SET    NewWatermark =  NEXT VALUE FOR dbo.WatermarkSequence
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1;
    COMMIT TRANSACTION;
    SELECT i.StudyInstanceUid,
           i.SeriesInstanceUid,
           i.SopInstanceUid,
           i.Watermark,
           i.TransferSyntaxUid,
           i.HasFrameMetadata,
           i.OriginalWatermark,
           i.NewWatermark,
           f.FilePath,
           f.ETag
    FROM   dbo.Instance AS i
           LEFT OUTER JOIN
           dbo.FileProperty AS f
           ON f.InstanceKey = i.InstanceKey
              AND f.Watermark = i.Watermark
    WHERE  i.PartitionKey = @partitionKey
           AND i.StudyInstanceUid = @studyInstanceUid
           AND i.Status = 1;
END

GO
CREATE OR ALTER PROCEDURE dbo.CompleteReindexing
@extendedQueryTagKeys dbo.ExtendedQueryTagKeyTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE XQT
    SET    TagStatus = 1
    FROM   dbo.ExtendedQueryTag AS XQT
           INNER JOIN
           @extendedQueryTagKeys AS input
           ON XQT.TagKey = input.TagKey
    WHERE  TagStatus = 0;
    DELETE XQTO
    OUTPUT DELETED.TagKey
    FROM   dbo.ExtendedQueryTagOperation AS XQTO
           INNER JOIN
           dbo.ExtendedQueryTag AS XQT
           ON XQTO.TagKey = XQT.TagKey
           INNER JOIN
           @extendedQueryTagKeys AS input
           ON XQT.TagKey = input.TagKey
    WHERE  TagStatus = 1;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.DeleteDeletedInstance
@studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT
AS
SET NOCOUNT ON;
DELETE dbo.DeletedInstance
WHERE  StudyInstanceUid = @studyInstanceUid
       AND SeriesInstanceUid = @seriesInstanceUid
       AND SopInstanceUid = @sopInstanceUid
       AND Watermark = @watermark;

GO
CREATE OR ALTER PROCEDURE dbo.DeleteDeletedInstanceV6
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT
AS
SET NOCOUNT ON;
DELETE dbo.DeletedInstance
WHERE  PartitionKey = @partitionKey
       AND StudyInstanceUid = @studyInstanceUid
       AND SeriesInstanceUid = @seriesInstanceUid
       AND SopInstanceUid = @sopInstanceUid
       AND Watermark = @watermark;

GO
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTag
@tagPath VARCHAR (64), @dataType TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @tagStatus AS TINYINT;
    DECLARE @tagKey AS INT;
    SELECT @tagKey = TagKey,
           @tagStatus = TagStatus
    FROM   dbo.ExtendedQueryTag WITH (XLOCK)
    WHERE  dbo.ExtendedQueryTag.TagPath = @tagPath;
    IF @@ROWCOUNT = 0
        THROW 50404, 'extended query tag not found', 1;
    IF @tagStatus = 2
        THROW 50412, 'extended query tag is not in Ready or Adding status', 1;
    UPDATE dbo.ExtendedQueryTag
    SET    TagStatus = 2
    WHERE  dbo.ExtendedQueryTag.TagKey = @tagKey;
    COMMIT TRANSACTION;
    BEGIN TRANSACTION;
    IF @dataType = 0
        DELETE dbo.ExtendedQueryTagString
        WHERE  TagKey = @tagKey;
    ELSE
        IF @dataType = 1
            DELETE dbo.ExtendedQueryTagLong
            WHERE  TagKey = @tagKey;
        ELSE
            IF @dataType = 2
                DELETE dbo.ExtendedQueryTagDouble
                WHERE  TagKey = @tagKey;
            ELSE
                IF @dataType = 3
                    DELETE dbo.ExtendedQueryTagDateTime
                    WHERE  TagKey = @tagKey;
                ELSE
                    DELETE dbo.ExtendedQueryTagPersonName
                    WHERE  TagKey = @tagKey;
    DELETE dbo.ExtendedQueryTag
    WHERE  TagKey = @tagKey;
    DELETE dbo.ExtendedQueryTagError
    WHERE  TagKey = @tagKey;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagV16
@tagPath VARCHAR (64), @dataType TINYINT, @batchSize INT=1000
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @deletedRows AS INT;
    BEGIN TRANSACTION;
    DECLARE @tagKey AS INT;
    DECLARE @imageResourceType AS TINYINT = 0;
    SELECT @tagKey = TagKey
    FROM   dbo.ExtendedQueryTag WITH (XLOCK)
    WHERE  dbo.ExtendedQueryTag.TagPath = @tagPath;
    IF @@ROWCOUNT = 0
        THROW 50404, 'extended query tag not found', 1;
    UPDATE dbo.ExtendedQueryTag
    SET    TagStatus = 2
    WHERE  dbo.ExtendedQueryTag.TagKey = @tagKey;
    COMMIT TRANSACTION;
    SET @deletedRows = @batchSize;
    WHILE (@deletedRows = @batchSize)
        BEGIN
            EXECUTE dbo.ISleepIfBusy ;
            BEGIN TRANSACTION;
            IF @dataType = 0
                DELETE TOP (@batchSize)
                       dbo.ExtendedQueryTagString
                WHERE  TagKey = @tagKey
                       AND ResourceType = @imageResourceType;
            ELSE
                IF @dataType = 1
                    DELETE TOP (@batchSize)
                           dbo.ExtendedQueryTagLong
                    WHERE  TagKey = @tagKey
                           AND ResourceType = @imageResourceType;
                ELSE
                    IF @dataType = 2
                        DELETE TOP (@batchSize)
                               dbo.ExtendedQueryTagDouble
                        WHERE  TagKey = @tagKey
                               AND ResourceType = @imageResourceType;
                    ELSE
                        IF @dataType = 3
                            DELETE TOP (@batchSize)
                                   dbo.ExtendedQueryTagDateTime
                            WHERE  TagKey = @tagKey
                                   AND ResourceType = @imageResourceType;
                        ELSE
                            DELETE TOP (@batchSize)
                                   dbo.ExtendedQueryTagPersonName
                            WHERE  TagKey = @tagKey
                                   AND ResourceType = @imageResourceType;
            SET @deletedRows = @@ROWCOUNT;
            COMMIT TRANSACTION;
            CHECKPOINT;
        END
    SET @deletedRows = @batchSize;
    WHILE (@deletedRows = @batchSize)
        BEGIN
            EXECUTE dbo.ISleepIfBusy ;
            BEGIN TRANSACTION;
            DELETE TOP (@batchSize)
                   dbo.ExtendedQueryTagError
            WHERE  TagKey = @tagKey;
            SET @deletedRows = @@ROWCOUNT;
            COMMIT TRANSACTION;
            CHECKPOINT;
        END
    DELETE dbo.ExtendedQueryTag
    WHERE  TagKey = @tagKey;
END

GO
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagV8
@tagPath VARCHAR (64), @dataType TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @tagKey AS INT;
    DECLARE @imageResourceType AS TINYINT = 0;
    SELECT @tagKey = TagKey
    FROM   dbo.ExtendedQueryTag WITH (XLOCK)
    WHERE  dbo.ExtendedQueryTag.TagPath = @tagPath;
    IF @@ROWCOUNT = 0
        THROW 50404, 'extended query tag not found', 1;
    UPDATE dbo.ExtendedQueryTag
    SET    TagStatus = 2
    WHERE  dbo.ExtendedQueryTag.TagKey = @tagKey;
    COMMIT TRANSACTION;
    BEGIN TRANSACTION;
    IF @dataType = 0
        DELETE dbo.ExtendedQueryTagString
        WHERE  TagKey = @tagKey
               AND ResourceType = @imageResourceType;
    ELSE
        IF @dataType = 1
            DELETE dbo.ExtendedQueryTagLong
            WHERE  TagKey = @tagKey
                   AND ResourceType = @imageResourceType;
        ELSE
            IF @dataType = 2
                DELETE dbo.ExtendedQueryTagDouble
                WHERE  TagKey = @tagKey
                       AND ResourceType = @imageResourceType;
            ELSE
                IF @dataType = 3
                    DELETE dbo.ExtendedQueryTagDateTime
                    WHERE  TagKey = @tagKey
                           AND ResourceType = @imageResourceType;
                ELSE
                    DELETE dbo.ExtendedQueryTagPersonName
                    WHERE  TagKey = @tagKey
                           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagError
    WHERE  TagKey = @tagKey;
    DELETE dbo.ExtendedQueryTag
    WHERE  TagKey = @tagKey;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.DeleteInstanceV6
@cleanupAfter DATETIMEOFFSET (0), @createdStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @deletedInstances AS TABLE (
        PartitionKey      INT            ,
        StudyInstanceUid  VARCHAR (64)   ,
        SeriesInstanceUid VARCHAR (64)   ,
        SopInstanceUid    VARCHAR (64)   ,
        Status            TINYINT        ,
        Watermark         BIGINT         ,
        InstanceKey       INT            ,
        OriginalWatermark BIGINT         ,
        FilePath          NVARCHAR (4000) NULL,
        ETag              NVARCHAR (4000) NULL);
    DECLARE @studyKey AS BIGINT;
    DECLARE @seriesKey AS BIGINT;
    DECLARE @instanceKey AS BIGINT;
    DECLARE @deletedDate AS DATETIME2 = SYSUTCDATETIME();
    DECLARE @imageResourceType AS TINYINT = 0;
    SELECT @studyKey = StudyKey,
           @seriesKey = CASE @seriesInstanceUid WHEN NULL THEN NULL ELSE SeriesKey END,
           @instanceKey = CASE @sopInstanceUid WHEN NULL THEN NULL ELSE InstanceKey END
    FROM   dbo.Instance
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid);
    DELETE dbo.Instance
    OUTPUT deleted.PartitionKey, deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark, deleted.InstanceKey, deleted.OriginalWatermark, FP.FilePath, FP.ETag INTO @deletedInstances
    FROM   dbo.Instance AS i
           LEFT OUTER JOIN
           dbo.FileProperty AS FP
           ON i.InstanceKey = FP.InstanceKey
              AND i.Watermark = FP.Watermark
    WHERE  i.PartitionKey = @partitionKey
           AND i.StudyInstanceUid = @studyInstanceUid
           AND i.SeriesInstanceUid = ISNULL(@seriesInstanceUid, i.SeriesInstanceUid)
           AND i.SopInstanceUid = ISNULL(@sopInstanceUid, i.SopInstanceUid);
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance not found', 1;
    DELETE FP
    FROM   dbo.FileProperty AS FP
           INNER JOIN
           @deletedInstances AS DI
           ON DI.InstanceKey = FP.InstanceKey
              AND DI.Watermark = FP.Watermark;
    DECLARE @deletedTags AS TABLE (
        TagKey BIGINT);
    DELETE XQTE
    OUTPUT deleted.TagKey INTO @deletedTags
    FROM   dbo.ExtendedQueryTagError AS XQTE
           INNER JOIN
           @deletedInstances AS DI
           ON XQTE.Watermark = DI.Watermark;
    IF EXISTS (SELECT *
               FROM   @deletedTags)
        BEGIN
            DECLARE @deletedTagCounts AS TABLE (
                TagKey     BIGINT,
                ErrorCount INT   );
            INSERT INTO @deletedTagCounts (TagKey, ErrorCount)
            SELECT   TagKey,
                     COUNT(1)
            FROM     @deletedTags
            GROUP BY TagKey;
            UPDATE XQT
            SET    XQT.ErrorCount = XQT.ErrorCount - DTC.ErrorCount
            FROM   dbo.ExtendedQueryTag AS XQT
                   INNER JOIN
                   @deletedTagCounts AS DTC
                   ON XQT.TagKey = DTC.TagKey;
        END
    DELETE dbo.ExtendedQueryTagString
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagLong
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagDouble
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagDateTime
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagPersonName
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    INSERT INTO dbo.DeletedInstance (PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter, OriginalWatermark, FilePath, ETag)
    SELECT PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark,
           @deletedDate,
           0,
           @cleanupAfter,
           OriginalWatermark,
           FilePath,
           ETag
    FROM   @deletedInstances;
    IF NOT EXISTS (SELECT *
                   FROM   dbo.Instance WITH (HOLDLOCK, UPDLOCK)
                   WHERE  PartitionKey = @partitionKey
                          AND StudyKey = @studyKey
                          AND SeriesKey = ISNULL(@seriesKey, SeriesKey))
        BEGIN
            DELETE dbo.Series
            WHERE  StudyKey = @studyKey
                   AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
                   AND PartitionKey = @partitionKey;
            DELETE dbo.ExtendedQueryTagString
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagLong
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDouble
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDateTime
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagPersonName
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
        END
    IF NOT EXISTS (SELECT *
                   FROM   dbo.Series WITH (HOLDLOCK, UPDLOCK)
                   WHERE  Studykey = @studyKey
                          AND PartitionKey = @partitionKey)
        BEGIN
            DELETE dbo.Study
            WHERE  StudyKey = @studyKey
                   AND PartitionKey = @partitionKey;
            DELETE dbo.ExtendedQueryTagString
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagLong
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDouble
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDateTime
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagPersonName
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
        END
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 1,
           DI.PartitionKey,
           DI.StudyInstanceUid,
           DI.SeriesInstanceUid,
           DI.SopInstanceUid,
           DI.Watermark
    FROM   @deletedInstances AS DI
    WHERE  Status = @createdStatus;
    UPDATE CF
    SET    CF.CurrentWatermark = NULL
    FROM   dbo.ChangeFeed AS CF WITH (FORCESEEK)
           INNER JOIN
           @deletedInstances AS DI
           ON CF.PartitionKey = DI.PartitionKey
              AND CF.StudyInstanceUid = DI.StudyInstanceUid
              AND CF.SeriesInstanceUid = DI.SeriesInstanceUid
              AND CF.SopInstanceUid = DI.SopInstanceUid;
    COMMIT TRANSACTION;
    SELECT d.Watermark,
           d.partitionKey,
           d.studyInstanceUid,
           d.seriesInstanceUid,
           d.sopInstanceUid
    FROM   @deletedInstances AS d;
END

GO
CREATE OR ALTER PROCEDURE dbo.DeleteWorkitem
@partitionKey INT, @workitemUid VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @workitemResourceType AS TINYINT = 1;
    DECLARE @workitemKey AS BIGINT;
    SELECT @workitemKey = WorkitemKey
    FROM   dbo.Workitem
    WHERE  PartitionKey = @partitionKey
           AND WorkitemUid = @workitemUid;
    IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exists', 1;
    DELETE dbo.ExtendedQueryTagString
    WHERE  SopInstanceKey1 = @workitemKey
           AND PartitionKey = @partitionKey
           AND ResourceType = @workitemResourceType;
    DELETE dbo.ExtendedQueryTagLong
    WHERE  SopInstanceKey1 = @workitemKey
           AND PartitionKey = @partitionKey
           AND ResourceType = @workitemResourceType;
    DELETE dbo.ExtendedQueryTagDouble
    WHERE  SopInstanceKey1 = @workitemKey
           AND PartitionKey = @partitionKey
           AND ResourceType = @workitemResourceType;
    DELETE dbo.ExtendedQueryTagDateTime
    WHERE  SopInstanceKey1 = @workitemKey
           AND PartitionKey = @partitionKey
           AND ResourceType = @workitemResourceType;
    DELETE dbo.ExtendedQueryTagPersonName
    WHERE  SopInstanceKey1 = @workitemKey
           AND PartitionKey = @partitionKey
           AND ResourceType = @workitemResourceType;
    DELETE dbo.Workitem
    WHERE  WorkItemKey = @workitemKey
           AND PartitionKey = @partitionKey;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstance
@partitionKey INT, @studyInstanceUid VARCHAR (64), @patientId NVARCHAR (64)=NULL, @patientName NVARCHAR (325)=NULL, @patientBirthDate DATE=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @updatedInstances AS TABLE (
        PartitionKey      INT         ,
        StudyInstanceUid  VARCHAR (64),
        SeriesInstanceUid VARCHAR (64),
        SopInstanceUid    VARCHAR (64),
        Watermark         BIGINT      );
    DELETE @updatedInstances;
    UPDATE dbo.Instance
    SET    LastStatusUpdatedDate = @currentDate,
           OriginalWatermark     = ISNULL(OriginalWatermark, Watermark),
           Watermark             = NewWatermark,
           NewWatermark          = NULL
    OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.NewWatermark INTO @updatedInstances
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1
           AND NewWatermark IS NOT NULL;
    UPDATE dbo.Study
    SET    PatientId        = ISNULL(@patientId, PatientId),
           PatientName      = ISNULL(@patientName, PatientName),
           PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate)
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Study does not exist', 1;
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 2,
           PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   @updatedInstances;
    UPDATE C
    SET    CurrentWatermark = U.Watermark
    FROM   dbo.ChangeFeed AS C
           INNER JOIN
           @updatedInstances AS U
           ON C.PartitionKey = U.PartitionKey
              AND C.StudyInstanceUid = U.StudyInstanceUid
              AND C.SeriesInstanceUid = U.SeriesInstanceUid
              AND C.SopInstanceUid = U.SopInstanceUid;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstanceV44
@partitionKey INT, @studyInstanceUid VARCHAR (64), @patientId NVARCHAR (64)=NULL, @patientName NVARCHAR (325)=NULL, @patientBirthDate DATE=NULL, @insertFileProperties dbo.FilePropertyTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    CREATE TABLE #UpdatedInstances (
        PartitionKey      INT         ,
        StudyInstanceUid  VARCHAR (64),
        SeriesInstanceUid VARCHAR (64),
        SopInstanceUid    VARCHAR (64),
        Watermark         BIGINT      ,
        OriginalWatermark BIGINT      ,
        InstanceKey       BIGINT      
    );
    DELETE #UpdatedInstances;
    UPDATE dbo.Instance
    SET    LastStatusUpdatedDate = @currentDate,
           OriginalWatermark     = ISNULL(OriginalWatermark, Watermark),
           Watermark             = NewWatermark,
           NewWatermark          = NULL
    OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, inserted.Watermark, inserted.OriginalWatermark, deleted.InstanceKey INTO #UpdatedInstances
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1
           AND NewWatermark IS NOT NULL;
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstances')
        CREATE UNIQUE INDEX IXC_UpdatedInstances
            ON #UpdatedInstances(Watermark);
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstanceKeyWatermark')
        CREATE UNIQUE CLUSTERED INDEX IXC_UpdatedInstanceKeyWatermark
            ON #UpdatedInstances(InstanceKey, OriginalWatermark);
    UPDATE dbo.Study
    SET    PatientId        = ISNULL(@patientId, PatientId),
           PatientName      = ISNULL(@patientName, PatientName),
           PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate)
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Study does not exist', 1;
    IF EXISTS (SELECT 1
               FROM   @insertFileProperties)
        DELETE FP
        FROM   dbo.FileProperty AS FP
               INNER JOIN
               #UpdatedInstances AS U
               ON U.InstanceKey = FP.InstanceKey
        WHERE  U.OriginalWatermark != FP.Watermark;
    INSERT INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
    SELECT U.InstanceKey,
           I.Watermark,
           I.FilePath,
           I.ETag
    FROM   @insertFileProperties AS I
           INNER JOIN
           #UpdatedInstances AS U
           ON U.Watermark = I.Watermark;
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 2,
           PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   #UpdatedInstances;
    UPDATE C
    SET    CurrentWatermark = U.Watermark,
           FilePath         = I.FilePath
    FROM   dbo.ChangeFeed AS C
           INNER JOIN
           #UpdatedInstances AS U
           ON C.PartitionKey = U.PartitionKey
              AND C.StudyInstanceUid = U.StudyInstanceUid
              AND C.SeriesInstanceUid = U.SeriesInstanceUid
              AND C.SopInstanceUid = U.SopInstanceUid
           LEFT OUTER JOIN
           @insertFileProperties AS I
           ON I.Watermark = U.Watermark;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstanceV50
@partitionKey INT, @studyInstanceUid VARCHAR (64), @patientId NVARCHAR (64)=NULL, @patientName NVARCHAR (325)=NULL, @patientBirthDate DATE=NULL, @insertFileProperties dbo.FilePropertyTableType READONLY, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @resourceType AS TINYINT = 0;
    DECLARE @studyKey AS BIGINT;
    DECLARE @maxWatermark AS BIGINT;
    CREATE TABLE #UpdatedInstances (
        PartitionKey      INT         ,
        StudyInstanceUid  VARCHAR (64),
        SeriesInstanceUid VARCHAR (64),
        SopInstanceUid    VARCHAR (64),
        Watermark         BIGINT      ,
        OriginalWatermark BIGINT      ,
        InstanceKey       BIGINT      
    );
    DELETE #UpdatedInstances;
    UPDATE dbo.Instance
    SET    LastStatusUpdatedDate = @currentDate,
           OriginalWatermark     = ISNULL(OriginalWatermark, Watermark),
           Watermark             = NewWatermark,
           NewWatermark          = NULL
    OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, inserted.Watermark, inserted.OriginalWatermark, deleted.InstanceKey INTO #UpdatedInstances
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1
           AND NewWatermark IS NOT NULL;
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstances')
        CREATE UNIQUE INDEX IXC_UpdatedInstances
            ON #UpdatedInstances(Watermark);
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstanceKeyWatermark')
        CREATE UNIQUE CLUSTERED INDEX IXC_UpdatedInstanceKeyWatermark
            ON #UpdatedInstances(InstanceKey, OriginalWatermark);
    UPDATE dbo.Study
    SET    PatientId        = ISNULL(@patientId, PatientId),
           PatientName      = ISNULL(@patientName, PatientName),
           PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate),
           @studyKey        = StudyKey
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Study does not exist', 1;
    IF EXISTS (SELECT 1
               FROM   @insertFileProperties)
        DELETE FP
        FROM   dbo.FileProperty AS FP
               INNER JOIN
               #UpdatedInstances AS U
               ON U.InstanceKey = FP.InstanceKey
        WHERE  U.OriginalWatermark != FP.Watermark;
    INSERT INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
    SELECT U.InstanceKey,
           I.Watermark,
           I.FilePath,
           I.ETag
    FROM   @insertFileProperties AS I
           INNER JOIN
           #UpdatedInstances AS U
           ON U.Watermark = I.Watermark;
    SELECT @maxWatermark = max(Watermark)
    FROM   #UpdatedInstances;
    BEGIN TRY
        EXECUTE dbo.IIndexInstanceCoreV9 @partitionKey, @studyKey, NULL, NULL, @maxWatermark, @stringExtendedQueryTags, @longExtendedQueryTags, @doubleExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 2,
           PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   #UpdatedInstances;
    UPDATE C
    SET    CurrentWatermark = U.Watermark,
           FilePath         = I.FilePath
    FROM   dbo.ChangeFeed AS C
           INNER JOIN
           #UpdatedInstances AS U
           ON C.PartitionKey = U.PartitionKey
              AND C.StudyInstanceUid = U.StudyInstanceUid
              AND C.SeriesInstanceUid = U.SeriesInstanceUid
              AND C.SopInstanceUid = U.SopInstanceUid
           LEFT OUTER JOIN
           @insertFileProperties AS I
           ON I.Watermark = U.Watermark;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstanceV52
@partitionKey INT, @studyInstanceUid VARCHAR (64), @patientId NVARCHAR (64)=NULL, @patientName NVARCHAR (325)=NULL, @patientBirthDate DATE=NULL, @referringPhysicianName NVARCHAR (325)=NULL, @studyDescription NVARCHAR (64)=NULL, @accessionNumber NVARCHAR (64)=NULL, @insertFileProperties dbo.FilePropertyTableType READONLY, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @resourceType AS TINYINT = 0;
    DECLARE @studyKey AS BIGINT;
    DECLARE @maxWatermark AS BIGINT;
    CREATE TABLE #UpdatedInstances (
        PartitionKey      INT         ,
        StudyInstanceUid  VARCHAR (64),
        SeriesInstanceUid VARCHAR (64),
        SopInstanceUid    VARCHAR (64),
        Watermark         BIGINT      ,
        OriginalWatermark BIGINT      ,
        InstanceKey       BIGINT      
    );
    DELETE #UpdatedInstances;
    UPDATE dbo.Instance
    SET    LastStatusUpdatedDate = @currentDate,
           OriginalWatermark     = ISNULL(OriginalWatermark, Watermark),
           Watermark             = NewWatermark,
           NewWatermark          = NULL
    OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, inserted.Watermark, inserted.OriginalWatermark, deleted.InstanceKey INTO #UpdatedInstances
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1
           AND NewWatermark IS NOT NULL;
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstances')
        CREATE UNIQUE INDEX IXC_UpdatedInstances
            ON #UpdatedInstances(Watermark);
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstanceKeyWatermark')
        CREATE UNIQUE CLUSTERED INDEX IXC_UpdatedInstanceKeyWatermark
            ON #UpdatedInstances(InstanceKey, OriginalWatermark);
    UPDATE dbo.Study
    SET    PatientId              = ISNULL(@patientId, PatientId),
           PatientName            = ISNULL(@patientName, PatientName),
           PatientBirthDate       = ISNULL(@patientBirthDate, PatientBirthDate),
           ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName),
           StudyDescription       = ISNULL(@studyDescription, StudyDescription),
           AccessionNumber        = ISNULL(@accessionNumber, AccessionNumber),
           @studyKey              = StudyKey
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Study does not exist', 1;
    IF EXISTS (SELECT 1
               FROM   @insertFileProperties)
        DELETE FP
        FROM   dbo.FileProperty AS FP
               INNER JOIN
               #UpdatedInstances AS U
               ON U.InstanceKey = FP.InstanceKey
        WHERE  U.OriginalWatermark != FP.Watermark;
    INSERT INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
    SELECT U.InstanceKey,
           I.Watermark,
           I.FilePath,
           I.ETag
    FROM   @insertFileProperties AS I
           INNER JOIN
           #UpdatedInstances AS U
           ON U.Watermark = I.Watermark;
    SELECT @maxWatermark = max(Watermark)
    FROM   #UpdatedInstances;
    BEGIN TRY
        EXECUTE dbo.IIndexInstanceCoreV9 @partitionKey, @studyKey, NULL, NULL, @maxWatermark, @stringExtendedQueryTags, @longExtendedQueryTags, @doubleExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 2,
           PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   #UpdatedInstances;
    UPDATE C
    SET    CurrentWatermark = U.Watermark,
           FilePath         = I.FilePath
    FROM   dbo.ChangeFeed AS C
           INNER JOIN
           #UpdatedInstances AS U
           ON C.PartitionKey = U.PartitionKey
              AND C.StudyInstanceUid = U.StudyInstanceUid
              AND C.SeriesInstanceUid = U.SeriesInstanceUid
              AND C.SopInstanceUid = U.SopInstanceUid
           LEFT OUTER JOIN
           @insertFileProperties AS I
           ON I.Watermark = U.Watermark;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstanceV54
@partitionKey INT, @studyInstanceUid VARCHAR (64), @patientId NVARCHAR (64)=NULL, @patientName NVARCHAR (325)=NULL, @patientBirthDate DATE=NULL, @referringPhysicianName NVARCHAR (325)=NULL, @studyDescription NVARCHAR (64)=NULL, @accessionNumber NVARCHAR (64)=NULL, @insertFileProperties dbo.FilePropertyTableType_2 READONLY, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @resourceType AS TINYINT = 0;
    DECLARE @studyKey AS BIGINT;
    DECLARE @maxWatermark AS BIGINT;
    CREATE TABLE #UpdatedInstances (
        PartitionKey      INT         ,
        StudyInstanceUid  VARCHAR (64),
        SeriesInstanceUid VARCHAR (64),
        SopInstanceUid    VARCHAR (64),
        Watermark         BIGINT      ,
        OriginalWatermark BIGINT      ,
        InstanceKey       BIGINT      
    );
    DELETE #UpdatedInstances;
    UPDATE dbo.Instance
    SET    LastStatusUpdatedDate = @currentDate,
           OriginalWatermark     = ISNULL(OriginalWatermark, Watermark),
           Watermark             = NewWatermark,
           NewWatermark          = NULL
    OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, inserted.Watermark, inserted.OriginalWatermark, deleted.InstanceKey INTO #UpdatedInstances
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND Status = 1
           AND NewWatermark IS NOT NULL;
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstances')
        CREATE UNIQUE INDEX IXC_UpdatedInstances
            ON #UpdatedInstances(Watermark);
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstanceKeyWatermark')
        CREATE UNIQUE CLUSTERED INDEX IXC_UpdatedInstanceKeyWatermark
            ON #UpdatedInstances(InstanceKey, OriginalWatermark);
    UPDATE dbo.Study
    SET    PatientId              = ISNULL(@patientId, PatientId),
           PatientName            = ISNULL(@patientName, PatientName),
           PatientBirthDate       = ISNULL(@patientBirthDate, PatientBirthDate),
           ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName),
           StudyDescription       = ISNULL(@studyDescription, StudyDescription),
           AccessionNumber        = ISNULL(@accessionNumber, AccessionNumber),
           @studyKey              = StudyKey
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Study does not exist', 1;
    IF EXISTS (SELECT 1
               FROM   @insertFileProperties)
        DELETE FP
        FROM   dbo.FileProperty AS FP
               INNER JOIN
               #UpdatedInstances AS U
               ON U.InstanceKey = FP.InstanceKey
        WHERE  U.OriginalWatermark != FP.Watermark;
    INSERT INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag, ContentLength)
    SELECT U.InstanceKey,
           I.Watermark,
           I.FilePath,
           I.ETag,
           I.ContentLength
    FROM   @insertFileProperties AS I
           INNER JOIN
           #UpdatedInstances AS U
           ON U.Watermark = I.Watermark;
    SELECT @maxWatermark = max(Watermark)
    FROM   #UpdatedInstances;
    BEGIN TRY
        EXECUTE dbo.IIndexInstanceCoreV9 @partitionKey, @studyKey, NULL, NULL, @maxWatermark, @stringExtendedQueryTags, @longExtendedQueryTags, @doubleExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 2,
           PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   #UpdatedInstances;
    UPDATE C
    SET    CurrentWatermark = U.Watermark,
           FilePath         = I.FilePath
    FROM   dbo.ChangeFeed AS C
           INNER JOIN
           #UpdatedInstances AS U
           ON C.PartitionKey = U.PartitionKey
              AND C.StudyInstanceUid = U.StudyInstanceUid
              AND C.SeriesInstanceUid = U.SeriesInstanceUid
              AND C.SopInstanceUid = U.SopInstanceUid
           LEFT OUTER JOIN
           @insertFileProperties AS I
           ON I.Watermark = U.Watermark;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeed
@limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   Sequence,
             Timestamp,
             Action,
             StudyInstanceUid,
             SeriesInstanceUid,
             SopInstanceUid,
             OriginalWatermark,
             CurrentWatermark
    FROM     dbo.ChangeFeed
    WHERE    Sequence BETWEEN @offset + 1 AND @offset + @limit
    ORDER BY Sequence;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedByTime
@startTime DATETIMEOFFSET (7), @endTime DATETIMEOFFSET (7), @limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   Sequence,
             Timestamp,
             Action,
             PartitionName,
             StudyInstanceUid,
             SeriesInstanceUid,
             SopInstanceUid,
             OriginalWatermark,
             CurrentWatermark
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    WHERE    c.Timestamp >= @startTime
             AND c.Timestamp < @endTime
    ORDER BY Timestamp, Sequence
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedByTimeV39
@startTime DATETIMEOFFSET (7), @endTime DATETIMEOFFSET (7), @limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   c.Sequence,
             c.Timestamp,
             c.Action,
             p.PartitionName,
             c.StudyInstanceUid,
             c.SeriesInstanceUid,
             c.SopInstanceUid,
             c.OriginalWatermark,
             c.CurrentWatermark,
             c.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    WHERE    c.Timestamp >= @startTime
             AND c.Timestamp < @endTime
    ORDER BY c.Timestamp, c.Sequence
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatest
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) Sequence,
                     Timestamp,
                     Action,
                     StudyInstanceUid,
                     SeriesInstanceUid,
                     SopInstanceUid,
                     OriginalWatermark,
                     CurrentWatermark
    FROM     dbo.ChangeFeed
    ORDER BY Sequence DESC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestByTime
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) Sequence,
                     Timestamp,
                     Action,
                     PartitionName,
                     StudyInstanceUid,
                     SeriesInstanceUid,
                     SopInstanceUid,
                     OriginalWatermark,
                     CurrentWatermark
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    ORDER BY Timestamp DESC, Sequence DESC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestByTimeV39
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) c.Sequence,
                     c.Timestamp,
                     c.Action,
                     p.PartitionName,
                     c.StudyInstanceUid,
                     c.SeriesInstanceUid,
                     c.SopInstanceUid,
                     c.OriginalWatermark,
                     c.CurrentWatermark,
                     c.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    ORDER BY c.Timestamp DESC, c.Sequence DESC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV39
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) c.Sequence,
                     c.Timestamp,
                     c.Action,
                     p.PartitionName,
                     c.StudyInstanceUid,
                     c.SeriesInstanceUid,
                     c.SopInstanceUid,
                     c.OriginalWatermark,
                     c.CurrentWatermark,
                     c.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    ORDER BY c.Sequence DESC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV6
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) Sequence,
                     Timestamp,
                     Action,
                     PartitionName,
                     StudyInstanceUid,
                     SeriesInstanceUid,
                     SopInstanceUid,
                     OriginalWatermark,
                     CurrentWatermark
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    ORDER BY Sequence DESC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV39
@limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   c.Sequence,
             c.Timestamp,
             c.Action,
             p.PartitionName,
             c.StudyInstanceUid,
             c.SeriesInstanceUid,
             c.SopInstanceUid,
             c.OriginalWatermark,
             c.CurrentWatermark,
             c.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    WHERE    c.Sequence BETWEEN @offset + 1 AND @offset + @limit
    ORDER BY c.Sequence;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV6
@limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   Sequence,
             Timestamp,
             Action,
             PartitionName,
             StudyInstanceUid,
             SeriesInstanceUid,
             SopInstanceUid,
             OriginalWatermark,
             CurrentWatermark
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
    WHERE    Sequence BETWEEN @offset + 1 AND @offset + @limit
    ORDER BY Sequence;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetContentLengthBackFillInstanceBatches
@batchSize INT, @batchCount INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT   MIN(Watermark) AS MinWatermark,
             MAX(Watermark) AS MaxWatermark
    FROM     (SELECT TOP (@batchSize * @batchCount) I.Watermark,
                                                    (ROW_NUMBER() OVER (ORDER BY I.Watermark DESC) - 1) / @batchSize AS Batch
              FROM   dbo.Instance AS I
                     INNER JOIN
                     dbo.FileProperty AS FP
                     ON FP.Watermark = I.Watermark
              WHERE  FP.ContentLength = 0) AS I
    GROUP BY Batch
    ORDER BY Batch ASC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetContentLengthBackFillInstanceIdentifiersByWatermarkRange
@startWatermark BIGINT, @endWatermark BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT I.StudyInstanceUid,
           I.SeriesInstanceUid,
           I.SopInstanceUid,
           I.Watermark,
           P.PartitionName,
           P.PartitionKey
    FROM   dbo.Instance AS I
           INNER JOIN
           dbo.Partition AS P
           ON P.PartitionKey = I.PartitionKey
           INNER JOIN
           dbo.FileProperty AS FP
           ON FP.Watermark = I.Watermark
    WHERE  I.Watermark BETWEEN @startWatermark AND @endWatermark
           AND FP.ContentLength = 0
           AND I.Status = 1;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetCurrentAndNextWorkitemWatermark
@workitemKey BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT WorkitemKey
                   FROM   dbo.Workitem
                   WHERE  WorkitemKey = @workitemKey)
        THROW 50409, 'Workitem does not exist', 1;
    DECLARE @proposedWatermark AS BIGINT;
    SET @proposedWatermark =  NEXT VALUE FOR dbo.WorkitemWatermarkSequence;
    SELECT Watermark,
           @proposedWatermark AS ProposedWatermark
    FROM   dbo.Workitem
    WHERE  WorkitemKey = @workitemKey;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTag
@tagPath VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM   dbo.ExtendedQueryTag AS XQT
           LEFT OUTER JOIN
           dbo.ExtendedQueryTagOperation AS XQTO
           ON XQT.TagKey = XQTO.TagKey
    WHERE  TagPath = ISNULL(@tagPath, TagPath);
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrors
@tagPath VARCHAR (64), @limit INT, @offset INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @tagKey AS INT;
    SELECT @tagKey = TagKey
    FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK)
    WHERE  dbo.ExtendedQueryTag.TagPath = @tagPath;
    IF (@@ROWCOUNT = 0)
        THROW 50404, 'extended query tag not found', 1;
    SELECT   TagKey,
             ErrorCode,
             CreatedTime,
             StudyInstanceUid,
             SeriesInstanceUid,
             SopInstanceUid
    FROM     dbo.ExtendedQueryTagError AS XQTE
             INNER JOIN
             dbo.Instance AS I
             ON XQTE.Watermark = I.Watermark
    WHERE    XQTE.TagKey = @tagKey
    ORDER BY CreatedTime ASC, XQTE.Watermark ASC, TagKey ASC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrorsV36
@tagPath VARCHAR (64), @limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @tagKey AS INT;
    SELECT @tagKey = TagKey
    FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK)
    WHERE  dbo.ExtendedQueryTag.TagPath = @tagPath;
    IF (@@ROWCOUNT = 0)
        THROW 50404, 'extended query tag not found', 1;
    SELECT   TagKey,
             ErrorCode,
             CreatedTime,
             PartitionName,
             StudyInstanceUid,
             SeriesInstanceUid,
             SopInstanceUid
    FROM     dbo.ExtendedQueryTagError AS XQTE
             INNER JOIN
             dbo.Instance AS I
             ON XQTE.Watermark = I.Watermark
             INNER JOIN
             dbo.Partition AS P
             ON P.PartitionKey = I.PartitionKey
    WHERE    XQTE.TagKey = @tagKey
    ORDER BY CreatedTime ASC, XQTE.Watermark ASC, TagKey ASC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrorsV6
@tagPath VARCHAR (64), @limit INT, @offset INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @tagKey AS INT;
    SELECT @tagKey = TagKey
    FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK)
    WHERE  dbo.ExtendedQueryTag.TagPath = @tagPath;
    IF (@@ROWCOUNT = 0)
        THROW 50404, 'extended query tag not found', 1;
    SELECT   TagKey,
             ErrorCode,
             CreatedTime,
             PartitionName,
             StudyInstanceUid,
             SeriesInstanceUid,
             SopInstanceUid
    FROM     dbo.ExtendedQueryTagError AS XQTE
             INNER JOIN
             dbo.Instance AS I
             ON XQTE.Watermark = I.Watermark
             INNER JOIN
             dbo.Partition AS P
             ON P.PartitionKey = I.PartitionKey
    WHERE    XQTE.TagKey = @tagKey
    ORDER BY CreatedTime ASC, XQTE.Watermark ASC, TagKey ASC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTags
@limit INT, @offset INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   XQT.TagKey,
             TagPath,
             TagVR,
             TagPrivateCreator,
             TagLevel,
             TagStatus,
             QueryStatus,
             ErrorCount,
             OperationId
    FROM     dbo.ExtendedQueryTag AS XQT
             LEFT OUTER JOIN
             dbo.ExtendedQueryTagOperation AS XQTO
             ON XQT.TagKey = XQTO.TagKey
    ORDER BY XQT.TagKey ASC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsByKey
@extendedQueryTagKeys dbo.ExtendedQueryTagKeyTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM   @extendedQueryTagKeys AS input
           INNER JOIN
           dbo.ExtendedQueryTag AS XQT
           ON input.TagKey = XQT.TagKey
           LEFT OUTER JOIN
           dbo.ExtendedQueryTagOperation AS XQTO
           ON XQT.TagKey = XQTO.TagKey;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsByOperation
@operationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount
    FROM   dbo.ExtendedQueryTag AS XQT
           INNER JOIN
           dbo.ExtendedQueryTagOperation AS XQTO
           ON XQT.TagKey = XQTO.TagKey
    WHERE  OperationId = @operationId;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsV36
@limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   XQT.TagKey,
             TagPath,
             TagVR,
             TagPrivateCreator,
             TagLevel,
             TagStatus,
             QueryStatus,
             ErrorCount,
             OperationId
    FROM     dbo.ExtendedQueryTag AS XQT
             LEFT OUTER JOIN
             dbo.ExtendedQueryTagOperation AS XQTO
             ON XQT.TagKey = XQTO.TagKey
    ORDER BY XQT.TagKey ASC
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetIndexedFileMetrics
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT COUNT_BIG(*) AS TotalIndexedFileCount,
           SUM(ContentLength) AS TotalIndexedBytes
    FROM   dbo.FileProperty;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstance
@validStatus TINYINT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   dbo.Instance
    WHERE  StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
           AND Status = @validStatus;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceBatches
@batchSize INT, @batchCount INT, @status TINYINT, @maxWatermark BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT   MIN(Watermark) AS MinWatermark,
             MAX(Watermark) AS MaxWatermark
    FROM     (SELECT TOP (@batchSize * @batchCount) Watermark,
                                                    (ROW_NUMBER() OVER (ORDER BY Watermark DESC) - 1) / @batchSize AS Batch
              FROM   dbo.Instance
              WHERE  Watermark <= ISNULL(@maxWatermark, Watermark)
                     AND Status = @status) AS I
    GROUP BY Batch
    ORDER BY Batch ASC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceBatchesByTimeStamp
@batchSize INT, @batchCount INT, @status TINYINT, @startTimeStamp DATETIMEOFFSET (0), @endTimeStamp DATETIMEOFFSET (0), @maxWatermark BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT   MIN(Watermark) AS MinWatermark,
             MAX(Watermark) AS MaxWatermark
    FROM     (SELECT TOP (@batchSize * @batchCount) Watermark,
                                                    (ROW_NUMBER() OVER (ORDER BY Watermark DESC) - 1) / @batchSize AS Batch
              FROM   dbo.Instance
              WHERE  Watermark <= ISNULL(@maxWatermark, Watermark)
                     AND Status = @status
                     AND CreatedDate >= @startTimeStamp
                     AND CreatedDate <= @endTimeStamp) AS I
    GROUP BY Batch
    ORDER BY Batch ASC;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstancesByWatermarkRange
@startWatermark BIGINT, @endWatermark BIGINT, @status TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   dbo.Instance
    WHERE  Watermark BETWEEN @startWatermark AND @endWatermark
           AND Status = @status;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstancesByWatermarkRangeV6
@startWatermark BIGINT, @endWatermark BIGINT, @status TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   dbo.Instance
    WHERE  Watermark BETWEEN @startWatermark AND @endWatermark
           AND Status = @status;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceV6
@validStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   dbo.Instance
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
           AND Status = @validStatus;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithProperties
@validStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark,
           TransferSyntaxUid,
           HasFrameMetadata
    FROM   dbo.Instance
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
           AND Status = @validStatus;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV32
@validStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark,
           TransferSyntaxUid,
           HasFrameMetadata,
           OriginalWatermark,
           NewWatermark
    FROM   dbo.Instance
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
           AND Status = @validStatus;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV46
@validStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT i.StudyInstanceUid,
           i.SeriesInstanceUid,
           i.SopInstanceUid,
           i.Watermark,
           i.TransferSyntaxUid,
           i.HasFrameMetadata,
           i.OriginalWatermark,
           i.NewWatermark,
           f.FilePath,
           f.ETag
    FROM   dbo.Instance AS i
           LEFT OUTER JOIN
           dbo.FileProperty AS f
           ON f.InstanceKey = i.InstanceKey
              AND f.Watermark = i.Watermark
    WHERE  i.PartitionKey = @partitionKey
           AND i.StudyInstanceUid = @studyInstanceUid
           AND i.SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND i.SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
           AND i.Status = @validStatus;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV58
@validStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL, @initialVersion BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT i.StudyInstanceUid,
           i.SeriesInstanceUid,
           i.SopInstanceUid,
           i.Watermark,
           i.TransferSyntaxUid,
           i.HasFrameMetadata,
           i.OriginalWatermark,
           i.NewWatermark,
           f.FilePath,
           f.ETag
    FROM   dbo.Instance AS i
           LEFT OUTER JOIN
           dbo.FileProperty AS f
           ON f.InstanceKey = i.InstanceKey
              AND f.Watermark = IIF (@initialVersion = 1, ISNULL(i.OriginalWatermark, i.Watermark), i.Watermark)
    WHERE  i.PartitionKey = @partitionKey
           AND i.StudyInstanceUid = @studyInstanceUid
           AND i.SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
           AND i.SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
           AND i.Status = @validStatus;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetPartition
@partitionName VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT PartitionKey,
           PartitionName,
           CreatedDate
    FROM   dbo.Partition
    WHERE  PartitionName = @partitionName;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetPartitions
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT PartitionKey,
           PartitionName,
           CreatedDate
    FROM   dbo.Partition;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetSeriesResult
@partitionKey INT, @watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT DISTINCT i.StudyInstanceUid,
                    sv.SeriesInstanceUid,
                    sv.Modality,
                    sv.PerformedProcedureStepStartDate,
                    sv.ManufacturerModelName,
                    sv.NumberofSeriesRelatedInstances
    FROM   dbo.Instance AS i
           INNER JOIN
           @watermarkTableType AS input
           ON i.Watermark = input.Watermark
              AND i.PartitionKey = @partitionKey
           INNER JOIN
           dbo.SeriesResultView AS sv
           ON i.StudyKey = sv.StudyKey
              AND i.SeriesKey = sv.SeriesKey
              AND i.PartitionKey = sv.PartitionKey;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetStudyResult
@partitionKey INT, @watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT DISTINCT sv.StudyInstanceUid,
                    sv.PatientId,
                    sv.PatientName,
                    sv.ReferringPhysicianName,
                    sv.StudyDate,
                    sv.StudyDescription,
                    sv.AccessionNumber,
                    sv.PatientBirthDate,
                    sv.ModalitiesInStudy,
                    sv.NumberofStudyRelatedInstances
    FROM   dbo.Instance AS i
           INNER JOIN
           @watermarkTableType AS input
           ON i.Watermark = input.Watermark
              AND i.PartitionKey = @partitionKey
           INNER JOIN
           dbo.StudyResultView AS sv
           ON i.StudyKey = sv.StudyKey
              AND i.PartitionKey = sv.PartitionKey;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetWorkitemMetadata
@partitionKey INT, @workitemUid VARCHAR (64), @procedureStepStateTagPath VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT wi.WorkitemUid,
           wi.WorkitemKey,
           wi.PartitionKey,
           wi.[Status],
           wi.TransactionUid,
           wi.Watermark,
           eqt.TagValue AS ProcedureStepState
    FROM   dbo.WorkitemQueryTag AS wqt
           INNER JOIN
           dbo.ExtendedQueryTagString AS eqt
           ON eqt.ResourceType = 1
              AND eqt.TagKey = wqt.TagKey
              AND wqt.TagPath = @procedureStepStateTagPath
           INNER JOIN
           dbo.Workitem AS wi
           ON wi.WorkitemKey = eqt.SopInstanceKey1
              AND wi.PartitionKey = eqt.PartitionKey
    WHERE  wi.PartitionKey = @partitionKey
           AND wi.WorkitemUid = @workitemUid;
    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem does not exist', 1;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetWorkitemQueryTags
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT TagKey,
           TagPath,
           TagVR
    FROM   dbo.WorkItemQueryTag;
END

GO
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCoreV9
@partitionKey INT=1, @studyKey BIGINT, @seriesKey BIGINT, @instanceKey BIGINT, @watermark BIGINT, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    DECLARE @resourceType AS TINYINT = 0;
    IF EXISTS (SELECT 1
               FROM   @stringExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagString
             AS T
            USING (SELECT input.TagKey,
                          input.TagValue,
                          input.TagLevel
                   FROM   @stringExtendedQueryTags AS input
                          INNER JOIN
                          dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                          ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                             AND dbo.ExtendedQueryTag.TagStatus <> 2) AS S ON T.ResourceType = @resourceType
                                                                              AND T.TagKey = S.TagKey
                                                                              AND T.PartitionKey = @partitionKey
                                                                              AND T.SopInstanceKey1 = @studyKey
                                                                              AND (T.SopInstanceKey2 IS NULL
                                                                                   OR T.SopInstanceKey2 = @seriesKey)
                                                                              AND (T.SopInstanceKey3 IS NULL
                                                                                   OR T.SopInstanceKey3 = @instanceKey)
            WHEN MATCHED AND @watermark > T.Watermark THEN UPDATE 
            SET T.Watermark = @watermark,
                T.TagValue  = ISNULL(S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType) VALUES (S.TagKey, S.TagValue, @partitionKey, @studyKey, (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END), (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END), @watermark, @resourceType);
        END
    IF EXISTS (SELECT 1
               FROM   @longExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagLong
             AS T
            USING (SELECT input.TagKey,
                          input.TagValue,
                          input.TagLevel
                   FROM   @longExtendedQueryTags AS input
                          INNER JOIN
                          dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                          ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                             AND dbo.ExtendedQueryTag.TagStatus <> 2) AS S ON T.ResourceType = @resourceType
                                                                              AND T.TagKey = S.TagKey
                                                                              AND T.PartitionKey = @partitionKey
                                                                              AND T.SopInstanceKey1 = @studyKey
                                                                              AND (T.SopInstanceKey2 IS NULL
                                                                                   OR T.SopInstanceKey2 = @seriesKey)
                                                                              AND (T.SopInstanceKey3 IS NULL
                                                                                   OR T.SopInstanceKey3 = @instanceKey)
            WHEN MATCHED AND @watermark > T.Watermark THEN UPDATE 
            SET T.Watermark = @watermark,
                T.TagValue  = ISNULL(S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType) VALUES (S.TagKey, S.TagValue, @partitionKey, @studyKey, (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END), (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END), @watermark, @resourceType);
        END
    IF EXISTS (SELECT 1
               FROM   @doubleExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagDouble
             AS T
            USING (SELECT input.TagKey,
                          input.TagValue,
                          input.TagLevel
                   FROM   @doubleExtendedQueryTags AS input
                          INNER JOIN
                          dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                          ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                             AND dbo.ExtendedQueryTag.TagStatus <> 2) AS S ON T.ResourceType = @resourceType
                                                                              AND T.TagKey = S.TagKey
                                                                              AND T.PartitionKey = @partitionKey
                                                                              AND T.SopInstanceKey1 = @studyKey
                                                                              AND (T.SopInstanceKey2 IS NULL
                                                                                   OR T.SopInstanceKey2 = @seriesKey)
                                                                              AND (T.SopInstanceKey3 IS NULL
                                                                                   OR T.SopInstanceKey3 = @instanceKey)
            WHEN MATCHED AND @watermark > T.Watermark THEN UPDATE 
            SET T.Watermark = @watermark,
                T.TagValue  = ISNULL(S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType) VALUES (S.TagKey, S.TagValue, @partitionKey, @studyKey, (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END), (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END), @watermark, @resourceType);
        END
    IF EXISTS (SELECT 1
               FROM   @dateTimeExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagDateTime
             AS T
            USING (SELECT input.TagKey,
                          input.TagValue,
                          input.TagValueUtc,
                          input.TagLevel
                   FROM   @dateTimeExtendedQueryTags AS input
                          INNER JOIN
                          dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                          ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                             AND dbo.ExtendedQueryTag.TagStatus <> 2) AS S ON T.ResourceType = @resourceType
                                                                              AND T.TagKey = S.TagKey
                                                                              AND T.PartitionKey = @partitionKey
                                                                              AND T.SopInstanceKey1 = @studyKey
                                                                              AND (T.SopInstanceKey2 IS NULL
                                                                                   OR T.SopInstanceKey2 = @seriesKey)
                                                                              AND (T.SopInstanceKey3 IS NULL
                                                                                   OR T.SopInstanceKey3 = @instanceKey)
            WHEN MATCHED AND @watermark > T.Watermark THEN UPDATE 
            SET T.Watermark = @watermark,
                T.TagValue  = ISNULL(S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, TagValueUtc, ResourceType) VALUES (S.TagKey, S.TagValue, @partitionKey, @studyKey, (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END), (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END), @watermark, S.TagValueUtc, @resourceType);
        END
    IF EXISTS (SELECT 1
               FROM   @personNameExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagPersonName
             AS T
            USING (SELECT input.TagKey,
                          input.TagValue,
                          input.TagLevel
                   FROM   @personNameExtendedQueryTags AS input
                          INNER JOIN
                          dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                          ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                             AND dbo.ExtendedQueryTag.TagStatus <> 2) AS S ON T.ResourceType = @resourceType
                                                                              AND T.TagKey = S.TagKey
                                                                              AND T.PartitionKey = @partitionKey
                                                                              AND T.SopInstanceKey1 = @studyKey
                                                                              AND (T.SopInstanceKey2 IS NULL
                                                                                   OR T.SopInstanceKey2 = @seriesKey)
                                                                              AND (T.SopInstanceKey3 IS NULL
                                                                                   OR T.SopInstanceKey3 = @instanceKey)
            WHEN MATCHED AND @watermark > T.Watermark THEN UPDATE 
            SET T.Watermark = @watermark,
                T.TagValue  = ISNULL(S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType) VALUES (S.TagKey, S.TagValue, @partitionKey, @studyKey, (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END), (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END), @watermark, @resourceType);
        END
END

GO
CREATE OR ALTER PROCEDURE dbo.IIndexWorkitemInstanceCore
@partitionKey INT=1, @workitemKey BIGINT, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    DECLARE @workitemResourceType AS TINYINT = 1;
    DECLARE @newWatermark AS BIGINT;
    SET @newWatermark =  NEXT VALUE FOR dbo.WatermarkSequence;
    IF EXISTS (SELECT 1
               FROM   @stringExtendedQueryTags)
        BEGIN
            INSERT dbo.ExtendedQueryTagString (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            SELECT input.TagKey,
                   input.TagValue,
                   @partitionKey,
                   @workitemKey,
                   NULL,
                   NULL,
                   @newWatermark,
                   @workitemResourceType
            FROM   @stringExtendedQueryTags AS input
                   INNER JOIN
                   dbo.WorkitemQueryTag
                   ON dbo.WorkitemQueryTag.TagKey = input.TagKey;
        END
    IF EXISTS (SELECT 1
               FROM   @dateTimeExtendedQueryTags)
        BEGIN
            INSERT dbo.ExtendedQueryTagDateTime (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            SELECT input.TagKey,
                   input.TagValue,
                   @partitionKey,
                   @workitemKey,
                   NULL,
                   NULL,
                   @newWatermark,
                   @workitemResourceType
            FROM   @dateTimeExtendedQueryTags AS input
                   INNER JOIN
                   dbo.WorkitemQueryTag
                   ON dbo.WorkitemQueryTag.TagKey = input.TagKey;
        END
    IF EXISTS (SELECT 1
               FROM   @personNameExtendedQueryTags)
        BEGIN
            INSERT dbo.ExtendedQueryTagPersonName (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            SELECT input.TagKey,
                   input.TagValue,
                   @partitionKey,
                   @workitemKey,
                   NULL,
                   NULL,
                   @newWatermark,
                   @workitemResourceType
            FROM   @personNameExtendedQueryTags AS input
                   INNER JOIN
                   dbo.WorkitemQueryTag
                   ON dbo.WorkitemQueryTag.TagKey = input.TagKey;
        END
END

GO
CREATE OR ALTER PROCEDURE dbo.IncrementDeletedInstanceRetry
@studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @cleanupAfter DATETIMEOFFSET (0)
AS
SET NOCOUNT ON;
DECLARE @retryCount AS INT;
UPDATE dbo.DeletedInstance
SET    @retryCount = RetryCount = RetryCount + 1,
       CleanupAfter             = @cleanupAfter
WHERE  StudyInstanceUid = @studyInstanceUid
       AND SeriesInstanceUid = @seriesInstanceUid
       AND SopInstanceUid = @sopInstanceUid
       AND Watermark = @watermark;
SELECT @retryCount;

GO
CREATE OR ALTER PROCEDURE dbo.IncrementDeletedInstanceRetryV6
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @cleanupAfter DATETIMEOFFSET (0)
AS
SET NOCOUNT ON;
DECLARE @retryCount AS INT;
UPDATE dbo.DeletedInstance
SET    @retryCount = RetryCount = RetryCount + 1,
       CleanupAfter             = @cleanupAfter
WHERE  PartitionKey = @partitionKey
       AND StudyInstanceUid = @studyInstanceUid
       AND SeriesInstanceUid = @seriesInstanceUid
       AND SopInstanceUid = @sopInstanceUid
       AND Watermark = @watermark;
SELECT @retryCount;

GO
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV6
@watermark BIGINT, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @partitionKey AS BIGINT;
    DECLARE @studyKey AS BIGINT;
    DECLARE @seriesKey AS BIGINT;
    DECLARE @instanceKey AS BIGINT;
    DECLARE @status AS TINYINT;
    SELECT @partitionKey = PartitionKey,
           @studyKey = StudyKey,
           @seriesKey = SeriesKey,
           @instanceKey = InstanceKey,
           @status = Status
    FROM   dbo.Instance WITH (UPDLOCK)
    WHERE  Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1;
    IF @status <> 1
        THROW 50409, 'Instance has not yet been stored succssfully', 1;
    DECLARE @maxTagLevel AS TINYINT;
    SELECT @maxTagLevel = MAX(TagLevel)
    FROM   (SELECT TagLevel
            FROM   @stringExtendedQueryTags
            UNION ALL
            SELECT TagLevel
            FROM   @longExtendedQueryTags
            UNION ALL
            SELECT TagLevel
            FROM   @doubleExtendedQueryTags
            UNION ALL
            SELECT TagLevel
            FROM   @dateTimeExtendedQueryTags
            UNION ALL
            SELECT TagLevel
            FROM   @personNameExtendedQueryTags) AS AllEntries;
    IF @maxTagLevel > 1
        BEGIN
            SELECT 1
            FROM   dbo.Study WITH (UPDLOCK)
            WHERE  PartitionKey = @partitionKey
                   AND StudyKey = @studyKey;
        END
    IF @maxTagLevel > 0
        BEGIN
            SELECT 1
            FROM   dbo.Series WITH (UPDLOCK)
            WHERE  PartitionKey = @partitionKey
                   AND StudyKey = @studyKey
                   AND SeriesKey = @seriesKey;
        END
    BEGIN TRY
        EXECUTE dbo.IIndexInstanceCoreV9 @partitionKey, @studyKey, @seriesKey, @instanceKey, @watermark, @stringExtendedQueryTags, @longExtendedQueryTags, @doubleExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.ISleepIfBusy
AS
BEGIN
    DECLARE @throttleCount AS INT;
    DECLARE @activeRequestCount AS INT;
    DECLARE @sleepersCount AS INT;
    DECLARE @throttleActiveRequestCount AS INT;
    IF (@@TRANCOUNT > 0)
        THROW 50400, 'Cannot sleep in transaction', 1;
    WHILE (1 = 1)
        BEGIN
            SELECT @throttleCount = ISNULL(SUM(CASE WHEN r.wait_type IN ('IO_QUEUE_LIMIT', 'LOG_RATE_GOVERNOR', 'SE_REPL_CATCHUP_THROTTLE', 'SE_REPL_SLOW_SECONDARY_THROTTLE', 'HADR_SYNC_COMMIT') THEN 1 ELSE 0 END), 0),
                   @sleepersCount = ISNULL(SUM(CASE WHEN r.wait_type IN ('WAITFOR') THEN 1 ELSE 0 END), 0),
                   @activeRequestCount = COUNT(*)
            FROM   sys.dm_exec_requests AS r WITH (NOLOCK)
                   INNER JOIN
                   sys.dm_exec_sessions AS s WITH (NOLOCK)
                   ON s.session_id = r.session_id
            WHERE  r.session_id <> @@spid
                   AND s.is_user_process = 1;
            SET @activeRequestCount = @activeRequestCount - @sleepersCount;
            IF (@throttleCount > 0)
                BEGIN
                    RAISERROR ('Throttling due to write waits', 10, 0)
                        WITH NOWAIT;
                    WAITFOR DELAY '00:00:02';
                END
            ELSE
                IF (@activeRequestCount >= 0)
                    BEGIN
                        IF (@throttleActiveRequestCount IS NULL)
                            BEGIN TRY
                                IF (OBJECT_ID('sys.dm_os_sys_info') IS NOT NULL)
                                    BEGIN
                                        SELECT @throttleActiveRequestCount = cpu_count * 3
                                        FROM   sys.dm_os_sys_info;
                                        IF (@throttleActiveRequestCount < 10)
                                            BEGIN
                                                SET @throttleActiveRequestCount = 10;
                                            END
                                        ELSE
                                            IF (@throttleActiveRequestCount > 100)
                                                BEGIN
                                                    SET @throttleActiveRequestCount = 100;
                                                END
                                    END
                            END TRY
                            BEGIN CATCH
                            END CATCH
                        IF (@throttleActiveRequestCount IS NULL)
                            BEGIN
                                SET @throttleActiveRequestCount = 20;
                            END
                        IF (@activeRequestCount > @throttleActiveRequestCount)
                            BEGIN
                                RAISERROR ('Throttling due to active requests being >= %d. Number of active requests = %d', 10, 0, @throttleActiveRequestCount, @activeRequestCount)
                                    WITH NOWAIT;
                                WAITFOR DELAY '00:00:01';
                            END
                        ELSE
                            BEGIN
                                BREAK;
                            END
                    END
                ELSE
                    BEGIN
                        BREAK;
                    END
        END
END

GO
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstance
@count INT, @maxRetries INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@count) StudyInstanceUid,
                        SeriesInstanceUid,
                        SopInstanceUid,
                        Watermark
    FROM   dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE  RetryCount <= @maxRetries
           AND CleanupAfter < SYSUTCDATETIME();
END

GO
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV42
@count INT, @maxRetries INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@count) p.PartitionName,
                        d.PartitionKey,
                        d.StudyInstanceUid,
                        d.SeriesInstanceUid,
                        d.SopInstanceUid,
                        d.Watermark,
                        d.OriginalWatermark,
                        d.FilePath,
                        d.ETag
    FROM   dbo.DeletedInstance AS d WITH (UPDLOCK, READPAST)
           INNER JOIN
           dbo.Partition AS p
           ON p.PartitionKey = d.PartitionKey
    WHERE  RetryCount <= @maxRetries
           AND CleanupAfter < SYSUTCDATETIME();
END

GO
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV6
@count INT, @maxRetries INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@count) PartitionKey,
                        StudyInstanceUid,
                        SeriesInstanceUid,
                        SopInstanceUid,
                        Watermark,
                        OriginalWatermark
    FROM   dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE  RetryCount <= @maxRetries
           AND CleanupAfter < SYSUTCDATETIME();
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateExtendedQueryTagQueryStatus
@tagPath VARCHAR (64), @queryStatus TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE XQT
    SET    QueryStatus = @queryStatus
    OUTPUT INSERTED.TagKey, INSERTED.TagPath, INSERTED.TagVR, INSERTED.TagPrivateCreator, INSERTED.TagLevel, INSERTED.TagStatus, INSERTED.QueryStatus, INSERTED.ErrorCount, XQTO.OperationId
    FROM   dbo.ExtendedQueryTag AS XQT
           LEFT OUTER JOIN
           dbo.ExtendedQueryTagOperation AS XQTO
           ON XQT.TagKey = XQTO.TagKey
    WHERE  TagPath = @tagPath;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateFilePropertiesContentLength
@filePropertiesToUpdate dbo.FilePropertyTableType_2 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE FP
    SET    ContentLength = FPTU.ContentLength
    FROM   dbo.FileProperty AS FP
           INNER JOIN
           @filePropertiesToUpdate AS FPTU
           ON FP.Watermark = FPTU.Watermark;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateFrameMetadata
@partitionKey INT, @hasFrameMetadata BIT, @watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.Instance
    SET    HasFrameMetadata = @hasFrameMetadata
    FROM   dbo.Instance AS i
           INNER JOIN
           @watermarkTableType AS input
           ON i.Watermark = input.Watermark
              AND i.PartitionKey = @partitionKey;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateIndexWorkitemInstanceCore
@workitemKey BIGINT, @partitionKey INT, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    DECLARE @workitemResourceType AS TINYINT = 1;
    DECLARE @newWatermark AS BIGINT;
    SET @newWatermark =  NEXT VALUE FOR dbo.WatermarkSequence;
    IF EXISTS (SELECT 1
               FROM   @stringExtendedQueryTags)
        BEGIN
            UPDATE ets
            SET    TagValue  = input.TagValue,
                   Watermark = @newWatermark
            FROM   dbo.ExtendedQueryTagString AS ets
                   INNER JOIN
                   @stringExtendedQueryTags AS input
                   ON ets.TagKey = input.TagKey
            WHERE  SopInstanceKey1 = @workitemKey
                   AND ResourceType = @workitemResourceType
                   AND PartitionKey = @partitionKey
                   AND ets.TagValue <> input.TagValue;
        END
    IF EXISTS (SELECT 1
               FROM   @dateTimeExtendedQueryTags)
        BEGIN
            UPDATE etdt
            SET    TagValue  = input.TagValue,
                   Watermark = @newWatermark
            FROM   dbo.ExtendedQueryTagDateTime AS etdt
                   INNER JOIN
                   @dateTimeExtendedQueryTags AS input
                   ON etdt.TagKey = input.TagKey
            WHERE  SopInstanceKey1 = @workitemKey
                   AND ResourceType = @workitemResourceType
                   AND PartitionKey = @partitionKey
                   AND etdt.TagValue <> input.TagValue;
        END
    IF EXISTS (SELECT 1
               FROM   @personNameExtendedQueryTags)
        BEGIN
            UPDATE etpn
            SET    TagValue  = input.TagValue,
                   Watermark = @newWatermark
            FROM   dbo.ExtendedQueryTagPersonName AS etpn
                   INNER JOIN
                   @personNameExtendedQueryTags AS input
                   ON etpn.TagKey = input.TagKey
            WHERE  SopInstanceKey1 = @workitemKey
                   AND ResourceType = @workitemResourceType
                   AND PartitionKey = @partitionKey
                   AND etpn.TagValue <> input.TagValue;
        END
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatus
@studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @status TINYINT
AS
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
UPDATE dbo.Instance
SET    Status                = @status,
       LastStatusUpdatedDate = @currentDate
WHERE  StudyInstanceUid = @studyInstanceUid
       AND SeriesInstanceUid = @seriesInstanceUid
       AND SopInstanceUid = @sopInstanceUid
       AND Watermark = @watermark;
IF @@ROWCOUNT = 0
    BEGIN
        THROW 50404, 'Instance does not exist', 1;
    END
INSERT  INTO dbo.ChangeFeed (Timestamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
VALUES                     (@currentDate, 0, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark);
UPDATE dbo.ChangeFeed
SET    CurrentWatermark = @watermark
WHERE  StudyInstanceUid = @studyInstanceUid
       AND SeriesInstanceUid = @seriesInstanceUid
       AND SopInstanceUid = @sopInstanceUid;
COMMIT TRANSACTION;

GO
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV37
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @status TINYINT, @maxTagKey INT=NULL, @hasFrameMetadata BIT=0, @path VARCHAR (4000)=NULL, @eTag VARCHAR (4000)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0)
                     FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @instanceKey AS BIGINT;
    UPDATE dbo.Instance
    SET    Status                = @status,
           LastStatusUpdatedDate = @CurrentDate,
           HasFrameMetadata      = @hasFrameMetadata,
           @instanceKey          = InstanceKey
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1;
    IF (@path IS NOT NULL
        AND @eTag IS NOT NULL
        AND @watermark IS NOT NULL)
        INSERT  INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
        VALUES                       (@instanceKey, @watermark, @path, @eTag);
    INSERT  INTO dbo.ChangeFeed (Timestamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark, FilePath)
    VALUES                     (@currentDate, 0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark, @path);
    UPDATE dbo.ChangeFeed
    SET    CurrentWatermark = @watermark
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV54
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @status TINYINT, @maxTagKey INT=NULL, @hasFrameMetadata BIT=0, @path VARCHAR (4000)=NULL, @eTag VARCHAR (4000)=NULL, @contentLength BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0)
                     FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @instanceKey AS BIGINT;
    UPDATE dbo.Instance
    SET    Status                = @status,
           LastStatusUpdatedDate = @CurrentDate,
           HasFrameMetadata      = @hasFrameMetadata,
           @instanceKey          = InstanceKey
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1;
    IF (@path IS NOT NULL
        AND @eTag IS NOT NULL
        AND @watermark IS NOT NULL)
        INSERT  INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag, ContentLength)
        VALUES                       (@instanceKey, @watermark, @path, @eTag, @contentLength);
    INSERT  INTO dbo.ChangeFeed (Timestamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark, FilePath)
    VALUES                     (@currentDate, 0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark, @path);
    UPDATE dbo.ChangeFeed
    SET    CurrentWatermark = @watermark
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV6
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @status TINYINT, @maxTagKey INT=NULL, @hasFrameMetadata BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0)
                     FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    UPDATE dbo.Instance
    SET    Status                = @status,
           LastStatusUpdatedDate = @currentDate,
           HasFrameMetadata      = @hasFrameMetadata
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1;
    INSERT  INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES                     (0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark);
    UPDATE dbo.ChangeFeed
    SET    CurrentWatermark = @watermark
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemProcedureStepState
@workitemKey BIGINT, @procedureStepStateTagPath VARCHAR (64), @procedureStepState VARCHAR (64), @watermark BIGINT, @proposedWatermark BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @newWatermark AS BIGINT;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @currentProcedureStepStateTagValue AS VARCHAR (64);
    UPDATE dbo.Workitem
    SET    Watermark = @proposedWatermark
    WHERE  WorkitemKey = @workitemKey
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem update failed.', 1;
    SET @newWatermark =  NEXT VALUE FOR dbo.WatermarkSequence;
    WITH TagKeyCTE
    AS   (SELECT wqt.TagKey,
                 wqt.TagPath,
                 eqts.TagValue AS OldTagValue,
                 eqts.ResourceType,
                 wi.PartitionKey,
                 wi.WorkitemKey,
                 eqts.Watermark AS ExtendedQueryTagWatermark
          FROM   dbo.WorkitemQueryTag AS wqt
                 INNER JOIN
                 dbo.ExtendedQueryTagString AS eqts
                 ON eqts.TagKey = wqt.TagKey
                    AND eqts.ResourceType = 1
                 INNER JOIN
                 dbo.Workitem AS wi
                 ON wi.PartitionKey = eqts.PartitionKey
                    AND wi.WorkitemKey = eqts.SopInstanceKey1
          WHERE  wi.WorkitemKey = @workitemKey)
    UPDATE targetTbl
    SET    targetTbl.TagValue  = @procedureStepState,
           targetTbl.Watermark = @newWatermark
    FROM   dbo.ExtendedQueryTagString AS targetTbl
           INNER JOIN
           TagKeyCTE AS cte
           ON targetTbl.ResourceType = cte.ResourceType
              AND cte.PartitionKey = targetTbl.PartitionKey
              AND cte.WorkitemKey = targetTbl.SopInstanceKey1
              AND cte.TagKey = targetTbl.TagKey
              AND cte.OldTagValue = targetTbl.TagValue
              AND cte.ExtendedQueryTagWatermark = targetTbl.Watermark
    WHERE  cte.TagPath = @procedureStepStateTagPath;
    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem procedure step state update failed.', 1;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemProcedureStepStateV21
@workitemKey BIGINT, @procedureStepStateTagPath VARCHAR (64), @procedureStepState VARCHAR (64), @watermark BIGINT, @proposedWatermark BIGINT, @transactionUid VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @newWatermark AS BIGINT;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @currentProcedureStepStateTagValue AS VARCHAR (64);
    UPDATE dbo.Workitem
    SET    Watermark      = @proposedWatermark,
           TransactionUid = @transactionUid
    WHERE  WorkitemKey = @workitemKey
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem update failed.', 1;
    SET @newWatermark =  NEXT VALUE FOR dbo.WatermarkSequence;
    WITH TagKeyCTE
    AS   (SELECT wqt.TagKey,
                 wqt.TagPath,
                 eqts.TagValue AS OldTagValue,
                 eqts.ResourceType,
                 wi.PartitionKey,
                 wi.WorkitemKey,
                 eqts.Watermark AS ExtendedQueryTagWatermark
          FROM   dbo.WorkitemQueryTag AS wqt
                 INNER JOIN
                 dbo.ExtendedQueryTagString AS eqts
                 ON eqts.TagKey = wqt.TagKey
                    AND eqts.ResourceType = 1
                 INNER JOIN
                 dbo.Workitem AS wi
                 ON wi.PartitionKey = eqts.PartitionKey
                    AND wi.WorkitemKey = eqts.SopInstanceKey1
          WHERE  wi.WorkitemKey = @workitemKey)
    UPDATE targetTbl
    SET    targetTbl.TagValue  = @procedureStepState,
           targetTbl.Watermark = @newWatermark
    FROM   dbo.ExtendedQueryTagString AS targetTbl
           INNER JOIN
           TagKeyCTE AS cte
           ON targetTbl.ResourceType = cte.ResourceType
              AND cte.PartitionKey = targetTbl.PartitionKey
              AND cte.WorkitemKey = targetTbl.SopInstanceKey1
              AND cte.TagKey = targetTbl.TagKey
              AND cte.OldTagValue = targetTbl.TagValue
              AND cte.ExtendedQueryTagWatermark = targetTbl.Watermark
    WHERE  cte.TagPath = @procedureStepStateTagPath;
    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem procedure step state update failed.', 1;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemStatus
@partitionKey INT, @workitemKey BIGINT, @status TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    UPDATE dbo.Workitem
    SET    Status                = @status,
           LastStatusUpdatedDate = @currentDate
    WHERE  PartitionKey = @partitionKey
           AND WorkitemKey = @workitemKey;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Workitem instance does not exist', 1;
    COMMIT TRANSACTION;
END

GO
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemTransaction
@workitemKey BIGINT, @partitionKey INT, @watermark BIGINT, @proposedWatermark BIGINT, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @newWatermark AS BIGINT;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    UPDATE dbo.Workitem
    SET    Watermark = @proposedWatermark
    WHERE  WorkitemKey = @workitemKey
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50499, 'Workitem update failed', 1;
    BEGIN TRY
        EXECUTE dbo.UpdateIndexWorkitemInstanceCore @workitemKey, @partitionKey, @stringExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
    COMMIT TRANSACTION;
END

GO
IF NOT EXISTS (SELECT *
               FROM   sys.views
               WHERE  Name = 'SeriesResultView')
    BEGIN
        EXECUTE ('CREATE VIEW dbo.SeriesResultView
    WITH SCHEMABINDING
    AS
    SELECT  se.SeriesInstanceUid,
            se.Modality,
            se.PerformedProcedureStepStartDate,
            se.ManufacturerModelName,
            (SELECT SUM(1)
            FROM dbo.Instance i 
            WHERE se.PartitionKey = i.PartitionKey
            AND se.StudyKey = i.StudyKey
            AND se.SeriesKey = i.SeriesKey) AS NumberofSeriesRelatedInstances,
            se.PartitionKey,
            se.StudyKey,
            se.SeriesKey
    FROM dbo.Series se');
    END

GO
IF NOT EXISTS (SELECT *
               FROM   sys.views
               WHERE  Name = 'StudyResultView')
    BEGIN
        EXECUTE ('CREATE VIEW dbo.StudyResultView
    WITH SCHEMABINDING
    AS
    SELECT  st.StudyInstanceUid,
            st.PatientId,
            st.PatientName,
            st.ReferringPhysicianName,
            st.StudyDate,
            st.StudyDescription,
            st.AccessionNumber,
            st.PatientBirthDate,
            (SELECT STRING_AGG(CONVERT(NVARCHAR(max), Modality), '','')
            FROM dbo.Series se 
            WHERE st.StudyKey = se.StudyKey
            AND st.PartitionKey = se.PartitionKey) AS ModalitiesInStudy,
            (SELECT SUM(1) 
            FROM dbo.Instance i 
            WHERE st.PartitionKey = i.PartitionKey
            AND st.StudyKey = i.StudyKey) AS NumberofStudyRelatedInstances,
            st.PartitionKey,
            st.StudyKey
    FROM dbo.Study st');
    END

GO
