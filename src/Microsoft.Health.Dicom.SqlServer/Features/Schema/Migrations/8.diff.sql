/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md

This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    Workitem Sequence
    Create sequence for workitem key
**************************************************************/
IF NOT EXISTS
(
    SELECT * FROM sys.sequences
    WHERE Name = 'WorkitemKeySequence'
)
BEGIN
    CREATE SEQUENCE dbo.WorkitemKeySequence
    AS INT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000
END

/*************************************************************
    Workitem Table
    Create table containing UPS-RS workitems.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.tables
    WHERE   Name = 'Workitem'
)
BEGIN
    CREATE TABLE dbo.Workitem (
        WorkitemKey                 BIGINT                            NOT NULL,             --PK
        PartitionKey                INT                               NOT NULL DEFAULT 1,   --FK
        WorkitemUid                 VARCHAR(64)                       NOT NULL,
        --audit columns
        CreatedDate                 DATETIME2(7)                      NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)

    -- Ordering workitems by partition and then by WorkitemKey for partition-specific retrieval
    CREATE UNIQUE CLUSTERED INDEX IXC_Workitem ON dbo.Workitem
    (
        PartitionKey,
        WorkitemKey
    )

    CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemUid_PartitionKey ON dbo.Workitem
    (
        WorkitemUid,
        PartitionKey
    )
    INCLUDE
    (
        WorkitemKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    ExtendedQueryTag Table
    Add ResourceType column.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'ResourceType'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTag')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTag
        ADD ResourceType TINYINT NOT NULL DEFAULT 0

    ALTER TABLE dbo.ExtendedQueryTag
        REBUILD WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    ExtendedQueryTagDateTime Table
    Add ResourceType column and rename columns for usage by both images and workitems.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'ResourceType'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagDateTime
        ADD ResourceType TINYINT NOT NULL DEFAULT 0

    EXEC sp_rename 'dbo.ExtendedQueryTagDateTime.StudyKey', 'SopInstanceKey1', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagDateTime.SeriesKey', 'SopInstanceKey2', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagDateTime.InstanceKey', 'SopInstanceKey3', 'COLUMN'

END

/*************************************************************
    ExtendedQueryTagDouble Table
    Add ResourceType column and rename columns for usage by both images and workitems.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'ResourceType'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagDouble
        ADD ResourceType TINYINT NOT NULL DEFAULT 0

    EXEC sp_rename 'dbo.ExtendedQueryTagDouble.StudyKey', 'SopInstanceKey1', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagDouble.SeriesKey', 'SopInstanceKey2', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagDouble.InstanceKey', 'SopInstanceKey3', 'COLUMN'

END

/*************************************************************
    ExtendedQueryTagLong Table
    Add ResourceType column and rename columns for usage by both images and workitems.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'ResourceType'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagLong
        ADD ResourceType TINYINT NOT NULL DEFAULT 0

    EXEC sp_rename 'dbo.ExtendedQueryTagLong.StudyKey', 'SopInstanceKey1', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagLong.SeriesKey', 'SopInstanceKey2', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagLong.InstanceKey', 'SopInstanceKey3', 'COLUMN'

END

/*************************************************************
    ExtendedQueryTagPersonName Table
    Add ResourceType column and rename columns for usage by both images and workitems.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'ResourceType'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagPersonName
        ADD ResourceType TINYINT NOT NULL DEFAULT 0

    EXEC sp_rename 'dbo.ExtendedQueryTagPersonName.StudyKey', 'SopInstanceKey1', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagPersonName.SeriesKey', 'SopInstanceKey2', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagPersonName.InstanceKey', 'SopInstanceKey3', 'COLUMN'

END

/*************************************************************
    ExtendedQueryTagString Table
    Add ResourceType column and rename columns for usage by both images and workitems.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'ResourceType'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagString
        ADD ResourceType TINYINT NOT NULL DEFAULT 0

    EXEC sp_rename 'dbo.ExtendedQueryTagString.StudyKey', 'SopInstanceKey1', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagString.SeriesKey', 'SopInstanceKey2', 'COLUMN'
    EXEC sp_rename 'dbo.ExtendedQueryTagString.InstanceKey', 'SopInstanceKey3', 'COLUMN'

END
GO

/*************************************************************
    Dropping obsolete stored procedures that refer to renamed columns 
**************************************************************/
DROP PROCEDURE IF EXISTS dbo.AddInstance
GO

DROP PROCEDURE IF EXISTS dbo.AddInstanceV2
GO

DROP PROCEDURE IF EXISTS dbo.DeleteInstance
GO

DROP PROCEDURE IF EXISTS dbo.IIndexInstanceCore
GO

DROP PROCEDURE IF EXISTS dbo.IndexInstance
GO

DROP PROCEDURE IF EXISTS dbo.IndexInstanceV2
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     AddExtendedQueryTags
--
-- DESCRIPTION
--    Adds a list of extended query tags. If a tag already exists, but it has yet to be assigned to a re-indexing
--    operation, then its existing row is deleted before the addition.
--
-- PARAMETERS
--     @extendedQueryTags
--         * The extended query tag list
--     @maxCount
--         * The max allowed extended query tag count
--     @ready
--         * Indicates whether the new query tags have been fully indexed
--
-- RETURN VALUE
--     The added extended query tags.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTags
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @maxAllowedCount INT = 128, -- Default value for backwards compatibility
    @ready BIT = 0
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON
BEGIN
    BEGIN TRANSACTION

        DECLARE @imageResourceType TINYINT = 0

        -- Check if total count exceed @maxCount
        -- HOLDLOCK to prevent adding queryTags from other transactions at same time.
        IF (SELECT COUNT(*)
            FROM dbo.ExtendedQueryTag AS XQT WITH(HOLDLOCK)
            FULL OUTER JOIN @extendedQueryTags AS input 
            ON XQT.TagPath = input.TagPath
            AND XQT.ResourceType = @imageResourceType) > @maxAllowedCount
            THROW 50409, 'extended query tags exceed max allowed count', 1

        -- Check if tag with same path already exist
        -- Because the web client may fail between the addition of the tag and the starting of re-indexing operation,
        -- the stored procedure allows tags that are not assigned to an operation to be overwritten
        DECLARE @existingTags TABLE(TagKey INT, TagStatus TINYINT, OperationId uniqueidentifier NULL)

        INSERT INTO @existingTags
            (TagKey, TagStatus, OperationId)
        SELECT XQT.TagKey, TagStatus, OperationId
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @extendedQueryTags AS input 
        ON input.TagPath = XQT.TagPath
        AND XQT.ResourceType = @imageResourceType
        LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO 
        ON XQT.TagKey = XQTO.TagKey

        IF EXISTS(
            SELECT 1 
            FROM @existingTags 
            WHERE TagStatus <> 0 
            OR (TagStatus = 0 AND OperationId IS NOT NULL))
            THROW 50409, 'extended query tag(s) already exist', 2

        -- Delete any "pending" tags whose operation has yet to be assigned
        DELETE XQT
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @existingTags AS et
        ON XQT.TagKey = et.TagKey

        -- Add the new tags with the given status
        INSERT INTO dbo.ExtendedQueryTag
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount)
        OUTPUT
            INSERTED.TagKey,
            INSERTED.TagPath,
            INSERTED.TagVR,
            INSERTED.TagPrivateCreator,
            INSERTED.TagLevel,
            INSERTED.TagStatus,
            INSERTED.QueryStatus,
            INSERTED.ErrorCount
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, @ready, 1, 0 FROM @extendedQueryTags

    COMMIT TRANSACTION
END
GO

/*************************************************************
    Stored procedure for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Adds a DICOM instance, now with partition.
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @patientId
--         * The Id of the patient.
--     @patientName
--         * The name of the patient.
--     @referringPhysicianName
--         * The referring physician name.
--     @studyDate
--         * The study date.
--     @studyDescription
--         * The study description.
--     @accessionNumber
--         * The accession number associated for the study.
--     @modality
--         * The modality associated for the series.
--     @performedProcedureStepStartDate
--         * The date when the procedure for the series was performed.
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @longExtendedQueryTags
--         * Long extended query tag data
--     @doubleExtendedQueryTags
--         * Double extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     The watermark (version).
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddInstanceV6
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @seriesInstanceUid                  VARCHAR(64),
    @sopInstanceUid                     VARCHAR(64),
    @patientId                          NVARCHAR(64),
    @patientName                        NVARCHAR(325) = NULL,
    @referringPhysicianName             NVARCHAR(325) = NULL,
    @studyDate                          DATE = NULL,
    @studyDescription                   NVARCHAR(64) = NULL,
    @accessionNumber                    NVARCHAR(64) = NULL,
    @modality                           NVARCHAR(16) = NULL,
    @performedProcedureStepStartDate    DATE = NULL,
    @patientBirthDate                   DATE = NULL,
    @manufacturerModelName              NVARCHAR(64) = NULL,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
    @initialStatus                      TINYINT
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @existingStatus TINYINT
    DECLARE @newWatermark BIGINT
    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT

    SELECT @existingStatus = Status
    FROM dbo.Instance
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid

    IF @@ROWCOUNT <> 0
        -- The instance already exists. Set the state = @existingStatus to indicate what state it is in.
        THROW 50409, 'Instance already exists', @existingStatus;

    -- The instance does not exist, insert it.
    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence
    SET @instanceKey = NEXT VALUE FOR dbo.InstanceKeySequence

    -- Insert Study
    SELECT @studyKey = StudyKey
    FROM dbo.Study WITH(UPDLOCK)
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

        INSERT INTO dbo.Study
            (PartitionKey, StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@partitionKey, @studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Study
        SET PatientId = @patientId, PatientName = @patientName, PatientBirthDate = @patientBirthDate, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
        WHERE StudyKey = @studyKey
    END

    -- Insert Series
    SELECT @seriesKey = SeriesKey
    FROM dbo.Series WITH(UPDLOCK)
    WHERE StudyKey = @studyKey
    AND SeriesInstanceUid = @seriesInstanceUid
    AND PartitionKey = @partitionKey

    IF @@ROWCOUNT = 0
    BEGIN
        SET @seriesKey = NEXT VALUE FOR dbo.SeriesKeySequence

        INSERT INTO dbo.Series
            (PartitionKey, StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate, ManufacturerModelName)
        VALUES
            (@partitionKey, @studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate, @manufacturerModelName)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Series
        SET Modality = @modality, PerformedProcedureStepStartDate = @performedProcedureStepStartDate, ManufacturerModelName = @manufacturerModelName
        WHERE SeriesKey = @seriesKey
        AND StudyKey = @studyKey
        AND PartitionKey = @partitionKey
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate)

    BEGIN TRY

        EXEC dbo.IIndexInstanceCoreV8
            @partitionKey,
            @studyKey,
            @seriesKey,
            @instanceKey,
            @newWatermark,
            @stringExtendedQueryTags,
            @longExtendedQueryTags,
            @doubleExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @newWatermark

    COMMIT TRANSACTION
END
GO

/*************************************************************
    Stored procedure for adding a workitem.
**************************************************************/
--
-- STORED PROCEDURE
--     AddWorkitem
--
-- DESCRIPTION
--     Adds a UPS-RS workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     The watermark (version).
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddWorkitem
    @partitionKey                       INT,
    @workitemUid                        VARCHAR(64),
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @newWatermark INT
    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @workitemKey BIGINT

    SELECT WorkitemUid
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', @workitemUid;

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    -- The workitem does not exist, insert it.
    SET @workitemKey = NEXT VALUE FOR dbo.WorkitemKeySequence
    INSERT INTO dbo.Workitem
        (WorkitemKey, PartitionKey, WorkitemUid, CreatedDate)
    VALUES
        (@workitemKey, @partitionKey, @workitemUid, @currentDate)

    BEGIN TRY

        EXEC dbo.IIndexInstanceCoreV8
            @partitionKey,
            @workitemKey,
            NULL,
            NULL,
            @newWatermark,
            @stringExtendedQueryTags,
            DEFAULT,
            DEFAULT,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags,
            @workitemResourceType

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @newWatermark

    COMMIT TRANSACTION
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTag
--
-- DESCRIPTION
--    Delete specific extended query tag
--
-- PARAMETERS
--     @tagPath
--         * The extended query tag path
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTag
    @tagPath VARCHAR(64),
    @dataType TINYINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @tagStatus TINYINT
        DECLARE @tagKey INT

        DECLARE @imageResourceType TINYINT = 0


        SELECT @tagKey = TagKey, @tagStatus = TagStatus
        FROM dbo.ExtendedQueryTag WITH(XLOCK)
        WHERE dbo.ExtendedQueryTag.TagPath = @tagPath
        AND dbo.ExtendedQueryTag.ResourceType = @imageResourceType

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1

        -- check if status is Ready or Adding
        IF @tagStatus = 2
            THROW 50412, 'extended query tag is not in Ready or Adding status', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION

    BEGIN TRANSACTION

        -- Delete index data
        IF @dataType = 0
            DELETE FROM dbo.ExtendedQueryTagString WHERE TagKey = @tagKey
        ELSE IF @dataType = 1
            DELETE FROM dbo.ExtendedQueryTagLong WHERE TagKey = @tagKey
        ELSE IF @dataType = 2
            DELETE FROM dbo.ExtendedQueryTagDouble WHERE TagKey = @tagKey
        ELSE IF @dataType = 3
            DELETE FROM dbo.ExtendedQueryTagDateTime WHERE TagKey = @tagKey
        ELSE
            DELETE FROM dbo.ExtendedQueryTagPersonName WHERE TagKey = @tagKey

        -- Delete tag
        DELETE FROM dbo.ExtendedQueryTag
        WHERE TagKey = @tagKey

        DELETE FROM dbo.ExtendedQueryTagError
        WHERE TagKey = @tagKey

    COMMIT TRANSACTION
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Removes the specified instance(s) and places them in the DeletedInstance table for later removal
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @cleanupAfter
--         * The date time offset that the instance can be cleaned up.
--     @createdStatus
--         * Status value representing the created state.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteInstanceV6
    @cleanupAfter       DATETIMEOFFSET(0),
    @createdStatus      TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

    DECLARE @deletedInstances AS TABLE
           (PartitionKey INT,
            StudyInstanceUid VARCHAR(64),
            SeriesInstanceUid VARCHAR(64),
            SopInstanceUid VARCHAR(64),
            Status TINYINT,
            Watermark BIGINT)

    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT
    DECLARE @deletedDate DATETIME2 = SYSUTCDATETIME()

    -- Get the study, series and instance PK
    SELECT  @studyKey = StudyKey,
    @seriesKey = CASE @seriesInstanceUid WHEN NULL THEN NULL ELSE SeriesKey END,
    @instanceKey = CASE @sopInstanceUid WHEN NULL THEN NULL ELSE InstanceKey END
    FROM    dbo.Instance
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    -- Delete the instance and insert the details into DeletedInstance and ChangeFeed
    DELETE  dbo.Instance
        OUTPUT deleted.PartitionKey, deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark
        INTO @deletedInstances
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance not found', 1

    -- Deleting tag errors
    DECLARE @deletedTags AS TABLE
    (
        TagKey BIGINT
    )
    DELETE XQTE
        OUTPUT deleted.TagKey
        INTO @deletedTags
    FROM dbo.ExtendedQueryTagError as XQTE
    INNER JOIN @deletedInstances AS DI
    ON XQTE.Watermark = DI.Watermark

    -- Update error count
    IF EXISTS (SELECT * FROM @deletedTags)
    BEGIN
        DECLARE @deletedTagCounts AS TABLE
        (
            TagKey BIGINT,
            ErrorCount INT
        )

        -- Calculate error count
        INSERT INTO @deletedTagCounts
            (TagKey, ErrorCount)
        SELECT TagKey, COUNT(1)
        FROM @deletedTags
        GROUP BY TagKey

        UPDATE XQT
        SET XQT.ErrorCount = XQT.ErrorCount - DTC.ErrorCount
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @deletedTagCounts AS DTC
        ON XQT.TagKey = DTC.TagKey
    END

    -- Deleting indexed instance tags
    DELETE
    FROM    dbo.ExtendedQueryTagString
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagLong
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagDouble
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagDateTime
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagPersonName
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey

    INSERT INTO dbo.DeletedInstance
    (PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter)
    SELECT PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, @deletedDate, 0 , @cleanupAfter
    FROM @deletedInstances

    INSERT INTO dbo.ChangeFeed
    (TimeStamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT @deletedDate, 1, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM @deletedInstances
    WHERE Status = @createdStatus

    UPDATE CF
    SET CF.CurrentWatermark = NULL
    FROM dbo.ChangeFeed AS CF WITH(FORCESEEK)
    JOIN @deletedInstances AS DI
    ON CF.PartitionKey = DI.PartitionKey
        AND CF.StudyInstanceUid = DI.StudyInstanceUid
        AND CF.SeriesInstanceUid = DI.SeriesInstanceUid
        AND CF.SopInstanceUid = DI.SopInstanceUid

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance WITH(HOLDLOCK, UPDLOCK)
                    WHERE   StudyKey = @studyKey
                    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid))
    BEGIN
        DELETE
        FROM    dbo.Series
        WHERE   StudyKey = @studyKey
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     PartitionKey = @partitionKey

        -- Deleting indexed series tags
        DELETE
        FROM    dbo.ExtendedQueryTagString
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey
    END

    -- If we've removing the series, see if it's the last for a study and if so, remove the study
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Series WITH(HOLDLOCK, UPDLOCK)
                    WHERE   Studykey = @studyKey
                    AND     PartitionKey = @partitionKey)
    BEGIN
        DELETE
        FROM    dbo.Study
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey

        -- Deleting indexed study tags
        DELETE
        FROM    dbo.ExtendedQueryTagString
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey
    END

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagErrorsV6
--
-- DESCRIPTION
--     Gets the extended query tag errors by tag path.
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag for which we retrieve error(s).
--     @limit
--         * The maximum number of results to retrieve.
--     @offset
--         * The offset from which to retrieve paginated results.
--
-- RETURN VALUE
--     The tag error fields and the corresponding instance UIDs.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrorsV6
    @tagPath VARCHAR(64),
    @limit   INT,
    @offset  INT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @tagKey INT
    DECLARE @imageResourceType TINYINT = 0

    SELECT @tagKey = TagKey
    FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)
    WHERE dbo.ExtendedQueryTag.TagPath = @tagPath
    AND ResourceType = @imageResourceType

    -- Check existence
    IF (@@ROWCOUNT = 0)
        THROW 50404, 'extended query tag not found', 1

    SELECT
        TagKey,
        ErrorCode,
        CreatedTime,
        PartitionName,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    FROM dbo.ExtendedQueryTagError AS XQTE
    INNER JOIN dbo.Instance AS I
    ON XQTE.Watermark = I.Watermark
    INNER JOIN dbo.Partition P
    ON P.PartitionKey = I.PartitionKey
    WHERE XQTE.TagKey = @tagKey
    ORDER BY CreatedTime ASC, XQTE.Watermark ASC, TagKey ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO


/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTag
--
-- DESCRIPTION
--     Gets all extended query tags or given extended query tag by tag path
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag to retrieve.
-- RETURN VALUE
--     The desired extended query tag, if found.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTag
    @tagPath  VARCHAR(64) = NULL -- Support NULL for backwards compatibility
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @imageResourceType TINYINT = 0

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM dbo.ExtendedQueryTag AS XQT
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    WHERE TagPath = ISNULL(@tagPath, TagPath)
    AND ResourceType = @imageResourceType
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagsByKey
--
-- DESCRIPTION
--     Gets the extended query tags by their respective keys.
--
-- PARAMETERS
--     @extendedQueryTagKeys
--         * The list of extended query tag keys.
-- RETURN VALUE
--     The corresponding extended query tags, if any.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsByKey
    @extendedQueryTagKeys dbo.ExtendedQueryTagKeyTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @imageResourceType TINYINT = 0

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM @extendedQueryTagKeys AS input
    INNER JOIN dbo.ExtendedQueryTag AS XQT ON input.TagKey = XQT.TagKey
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    WHERE XQT.ResourceType = @imageResourceType
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTags
--
-- DESCRIPTION
--     Gets a possibly paginated set of query tags as indicated by the parameters
--
-- PARAMETERS
--     @limit
--         * The maximum number of results to retrieve.
--     @offset
--         * The offset from which to retrieve paginated results.
--
-- RETURN VALUE
--     The set of query tags.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTags
    @limit INT,
    @offset INT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @imageResourceType TINYINT = 0

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM dbo.ExtendedQueryTag AS XQT
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    WHERE XQT.ResourceType = @imageResourceType
    ORDER BY XQT.TagKey ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    IIndexInstanceCoreV8
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @sopInstanceKey1
--         * Refers to either StudyKey or WorkItemKey depending on ResourceType
--     @sopInstanceKey2
--         * Refers to SeriesKey if ResourceType is Image else NULL
--     @sopInstanceKey3
--         * Refers to InstanceKey if ResourceType is Image else NULL
--     @watermark
--         * The DICOM instance watermark
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @longExtendedQueryTags
--         * Long extended query tag data
--     @doubleExtendedQueryTags
--         * Double extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
--     @resourceType
--         * The resource type that owns these tags: 0 = Image, 1 = Workitem. Default is Image
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCoreV8
    @partitionKey                                                                INT = 1,
    @sopInstanceKey1                                                             BIGINT,
    @sopInstanceKey2                                                             BIGINT,
    @sopInstanceKey3                                                             BIGINT,
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
    @resourceType                                                                TINYINT = 0
AS
BEGIN
    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagString WITH (HOLDLOCK) AS T
        USING
        (
            -- Locks tags in dbo.ExtendedQueryTag
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @stringExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
            );
    END

    -- Long Key tags
    IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagLong WITH (HOLDLOCK) AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @longExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
            );
    END

    -- Double Key tags
    IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagDouble WITH (HOLDLOCK) AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @doubleExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
            );
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagDateTime WITH (HOLDLOCK) AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagValueUtc, input.TagLevel
            FROM @dateTimeExtendedQueryTags input
           INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, TagValueUtc, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                S.TagValueUtc,
                @resourceType
            );
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagPersonName WITH (HOLDLOCK) AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @personNameExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
            );
    END
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance V6
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance.
--
-- PARAMETERS
--     @watermark
--         * The Dicom instance watermark.
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @longExtendedQueryTags
--         * Long extended query tag data
--     @doubleExtendedQueryTags
--         * Double extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV6
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT    ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @partitionKey BIGINT
        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT

        -- Add lock so that the instance cannot be removed
        DECLARE @status TINYINT
        SELECT
            @partitionKey = PartitionKey,
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @status = Status
        FROM dbo.Instance WITH (HOLDLOCK)
        WHERE Watermark = @watermark

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exists', 1
        IF @status <> 1 -- Created
            THROW 50409, 'Instance has not yet been stored succssfully', 1

        -- Insert Extended Query Tags

        -- String Key tags
        BEGIN TRY

            EXEC dbo.IIndexInstanceCoreV8
                @partitionKey,
                @studyKey,
                @seriesKey,
                @instanceKey,
                @watermark,
                @stringExtendedQueryTags,
                @longExtendedQueryTags,
                @doubleExtendedQueryTags,
                @dateTimeExtendedQueryTags,
                @personNameExtendedQueryTags

        END TRY
        BEGIN CATCH

            THROW

        END CATCH

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION

BEGIN TRANSACTION
BEGIN TRY
-- wrapping the contents of this transaction in try/catch because errors on index
-- operations won't rollback unless caught and re-thrown
    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTag_TagPath'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTag')
    )
    BEGIN
        DROP INDEX IX_ExtendedQueryTag_TagPath ON dbo.ExtendedQueryTag

        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTag_TagPath_ResourceType ON dbo.ExtendedQueryTag
        (
            TagPath,
            ResourceType
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_StudyKey_SeriesKey_InstanceKey'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
    )
    BEGIN
        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
        (
            TagKey,
            TagValue,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        ) WITH (DROP_EXISTING = ON)
    
        DROP INDEX IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDateTime
        
        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDateTime
        (
            TagKey,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagDateTime_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDateTime
        
        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDateTime
        (
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTagDouble_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
    )
    BEGIN
        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDouble ON dbo.ExtendedQueryTagDouble
        (
            TagKey,
            TagValue,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        ) WITH (DROP_EXISTING = ON)
    
        DROP INDEX IX_ExtendedQueryTagDouble_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDouble
        
        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDouble
        (
            TagKey,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagDouble_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDouble
        
        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDouble
        (
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTagLong_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
    )
    BEGIN
        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagLong ON dbo.ExtendedQueryTagLong
        (
            TagKey,
            TagValue,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        ) WITH (DROP_EXISTING = ON)
    
        DROP INDEX IX_ExtendedQueryTagLong_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagLong
        
        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagLong
        (
            TagKey,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagLong_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagLong
        
        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagLong
        (
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
    )
    BEGIN
        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName ON dbo.ExtendedQueryTagPersonName
        (
            TagKey,
            TagValue,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        ) WITH (DROP_EXISTING = ON)
    
        DROP INDEX IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagPersonName
        
        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagPersonName
        (
            TagKey,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagPersonName_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagPersonName
        
        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagPersonName
        (
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTagString_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
    )
    BEGIN
        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagString ON dbo.ExtendedQueryTagString
        (
            TagKey,
            TagValue,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        ) WITH (DROP_EXISTING = ON)
    
        DROP INDEX IX_ExtendedQueryTagString_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagString
        
        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagString_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagString
        (
            TagKey,
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagString_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagString
        
        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagString
        (
            PartitionKey,
            ResourceType,
            SopInstanceKey1,
            SopInstanceKey2,
            SopInstanceKey3
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    COMMIT TRANSACTION

END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;
END CATCH
