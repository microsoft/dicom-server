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
        TransactionUid               VARCHAR(64)                      NULL,
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
        WorkitemKey,
        TransactionUid
    )
    WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    Workitem Query Tag Table
    Stores static workitem indexed tags
    TagPath is represented with delimiters to repesent multiple levels
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.tables
    WHERE   Name = 'WorkitemQueryTag'
)
BEGIN
    CREATE TABLE dbo.WorkitemQueryTag (
        TagKey                  INT                  NOT NULL, --PK
        TagPath                 VARCHAR(64)          NOT NULL,
        TagVR                   VARCHAR(2)           NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)

    CREATE UNIQUE CLUSTERED INDEX IXC_WorkitemQueryTag ON dbo.WorkitemQueryTag
    (
        TagKey
    )

    CREATE UNIQUE NONCLUSTERED INDEX IXC_WorkitemQueryTag_TagPath ON dbo.WorkitemQueryTag
    (
        TagPath
    )
    WITH (DATA_COMPRESSION = PAGE)
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

        EXEC dbo.IIndexInstanceCoreV9
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
--     The WorkitemKey
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddWorkitem
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @workitemKey BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', 1;

    -- The workitem does not exist, insert it.
    SET @workitemKey = NEXT VALUE FOR dbo.WorkitemKeySequence
    INSERT INTO dbo.Workitem
        (WorkitemKey, PartitionKey, WorkitemUid, CreatedDate)
    VALUES
        (@workitemKey, @partitionKey, @workitemUid, @currentDate)

    BEGIN TRY

        EXEC dbo.IIndexWorkitemInstanceCore
            @partitionKey,
            @workitemKey,
            @stringExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @workitemKey

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
--    DeleteWorkitem
--
-- DESCRIPTION
--    Delete specific workitem and its query tag values
--
-- PARAMETERS
--     @partitionKey
--         * The Partition Key
--     @workitemUid
--         * The workitem UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteWorkitem
    @partitionKey INT,
    @workitemUid  VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @workitemResourceType TINYINT = 1
        DECLARE @workitemKey BIGINT

        SELECT @workitemKey = WorkitemKey
        FROM dbo.Workitem
        WHERE PartitionKey = @partitionKey
            AND WorkitemUid = @workitemUid

        -- Check existence
        IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exists', 1;


        DELETE FROM dbo.ExtendedQueryTagString
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagLong
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagDouble
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagDateTime
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagPersonName
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType


        DELETE FROM dbo.Workitem
        WHERE WorkItemKey = @workitemKey
            AND PartitionKey = @partitionKey

    COMMIT TRANSACTION
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetWorkitemQueryTags
--
-- DESCRIPTION
--     Gets indexed workitem query tags
--
-- RETURN VALUE
--     The set of query tags.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetWorkitemQueryTags
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT TagKey,
           TagPath,
           TagVR
    FROM dbo.WorkItemQueryTag
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    IIndexInstanceCoreV9
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @studyKey
--         * The internal key for the study
--     @seriesKey
--         * The internal key for the series
--     @instanceKey
--         * The internal key for the instance
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
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCoreV9
    @partitionKey                                                                INT = 1,
    @studyKey                                                                    BIGINT,
    @seriesKey                                                                   BIGINT,
    @instanceKey                                                                 BIGINT,
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    DECLARE @resourceType TINYINT = 0
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
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
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
                @studyKey,
                -- When TagLevel is not Study, we should fill SeriesKey
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                -- When TagLevel is Instance, we should fill InstanceKey
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
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
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
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
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
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
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
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
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
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
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
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
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
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
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
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
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                @resourceType
            );
    END
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    IIndexWorkitemInstanceCore
--
-- DESCRIPTION
--    Adds workitem query tag values
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @workitemKey
--         * Refers to WorkItemKey
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexWorkitemInstanceCore
    @partitionKey                                                                INT = 1,
    @workitemKey                                                                 BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN

    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @newWatermark BIGINT

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        INSERT dbo.ExtendedQueryTagString (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
        SELECT input.TagKey, input.TagValue, @partitionKey, @workitemKey, NULL, NULL, @newWatermark,@workitemResourceType
        FROM @stringExtendedQueryTags input
        INNER JOIN dbo.WorkitemQueryTag
        ON dbo.WorkitemQueryTag.TagKey = input.TagKey
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
        INSERT dbo.ExtendedQueryTagDateTime (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
        SELECT input.TagKey, input.TagValue, @partitionKey, @workitemKey, NULL, NULL, @newWatermark,@workitemResourceType
        FROM @dateTimeExtendedQueryTags input
        INNER JOIN dbo.WorkitemQueryTag
        ON dbo.WorkitemQueryTag.TagKey = input.TagKey
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
        INSERT dbo.ExtendedQueryTagPersonName (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
        SELECT input.TagKey, input.TagValue, @partitionKey, @workitemKey, NULL, NULL, @newWatermark,@workitemResourceType
        FROM @personNameExtendedQueryTags input
        INNER JOIN dbo.WorkitemQueryTag
        ON dbo.WorkitemQueryTag.TagKey = input.TagKey
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

            EXEC dbo.IIndexInstanceCoreV9
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

/*************************************************************
    Stored procedure to update a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpateWorkitem
--
-- DESCRIPTION
--     Update a UPS-RS Workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
--     @procedure​Step​StateTagPath
--         * Procedure Step State Tag Path
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpateWorkitem
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @procedureStepStateTagPath      VARCHAR(64),

    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @newWatermark BIGINT

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    DECLARE @workitemKey BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exists', 1;

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        UPDATE dbo.ExtendedQueryTagString
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        WHERE
            dbo.ExtendedQueryTagString.SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND TagKey = (SELECT input.TagKey
                FROM @stringExtendedQueryTags input
                INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey)
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
        UPDATE dbo.ExtendedQueryTagDateTime
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        WHERE
            dbo.ExtendedQueryTagDateTime.SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND TagKey = (SELECT input.TagKey
                FROM @dateTimeExtendedQueryTags input
                INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey)
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
        UPDATE dbo.ExtendedQueryTagPersonName
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        WHERE
            dbo.ExtendedQueryTagPersonName.SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND TagKey = (SELECT input.TagKey
                FROM @personNameExtendedQueryTags input
                INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey)
    END

    COMMIT TRANSACTION
END
GO

/*************************************************************
    Stored procedure to UpdateWorkitem a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitem
--
-- DESCRIPTION
--     Update a UPS-RS Workitem.
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
--     None
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitem
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),

    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @newWatermark BIGINT

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    DECLARE @workitemKey BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exist', 1;

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
		WITH InputCTE AS (
			SELECT 
				input.TagValue,
				input.TagKey
			FROM 
				@stringExtendedQueryTags input
				INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey
		)
        UPDATE dbo.ExtendedQueryTagString
        SET
            TagValue = cte.TagValue,
            Watermark = @newWatermark
        FROM
			dbo.ExtendedQueryTagString t
			INNER JOIN InputCTE cte ON cte.TagKey = t.TagKey
		WHERE
            SopInstanceKey1 = @workitemKey
			AND PartitionKey = @partitionKey
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
		WITH InputCTE AS (
			SELECT 
				input.TagValue,
				input.TagKey
			FROM 
				@dateTimeExtendedQueryTags input
				INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey
		)
        UPDATE dbo.ExtendedQueryTagDateTime
        SET
            TagValue = cte.TagValue,
            Watermark = @newWatermark
        FROM
			dbo.ExtendedQueryTagDateTime t
			INNER JOIN InputCTE cte ON cte.TagKey = t.TagKey
		WHERE
            SopInstanceKey1 = @workitemKey
			AND PartitionKey = @partitionKey
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
		WITH InputCTE AS (
			SELECT 
				input.TagValue,
				input.TagKey
			FROM 
				@personNameExtendedQueryTags input
				INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey
		)
        UPDATE dbo.ExtendedQueryTagPersonName
        SET
            TagValue = cte.TagValue,
            Watermark = @newWatermark
        FROM
			dbo.ExtendedQueryTagPersonName t
			INNER JOIN InputCTE cte ON cte.TagKey = t.TagKey
		WHERE
            SopInstanceKey1 = @workitemKey
			AND PartitionKey = @partitionKey
    END

    COMMIT TRANSACTION

    SELECT @workitemKey

END
GO

/*************************************************************
    Stored procedure for getting a workitem detail.
**************************************************************/
--
-- STORED PROCEDURE
--     GetWorkitemDetail
--
-- DESCRIPTION
--     Gets a UPS-RS workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
-- RETURN VALUE
--     The WorkitemKey
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetWorkitemDetail
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT
	    wi.WorkitemUid,
	    wi.WorkitemKey,
	    wi.PartitionKey,
	    eqt.TagValue AS ProcedureStepState
    FROM 
	    dbo.ExtendedQueryTagString eqt
	    INNER JOIN dbo.WorkitemQueryTag wqt
            ON wqt.TagKey = eqt.TagKey AND wqt.TagPath = '00741000' -- TagPath for Procedure Step State
	    INNER JOIN dbo.Workitem wi
            ON wi.WorkitemKey = eqt.SopInstanceKey1 AND wi.PartitionKey = eqt.PartitionKey
    WHERE
        eqt.ResourceType = 1
	    AND wi.PartitionKey = @partitionKey
	    AND wi.WorkitemUid = @workitemUid

END
GO

COMMIT TRANSACTION

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_ExtendedQueryTagDateTime'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
    (
        ResourceType,
        TagKey,
        TagValue,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    ) WITH (DROP_EXISTING = ON, ONLINE = ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDateTime
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDateTime
    (
        ResourceType,
        TagKey,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDateTime_PartitionKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagDateTime_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDateTime
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDateTime_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDateTime
    (
        ResourceType,
        PartitionKey,
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
    WHERE   NAME = 'IXC_ExtendedQueryTagDouble'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDouble ON dbo.ExtendedQueryTagDouble
    (
        ResourceType,
        TagKey,
        TagValue,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    ) WITH (DROP_EXISTING = ON, ONLINE = ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDouble_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagDouble_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDouble
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDouble_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDouble
    (
        ResourceType,
        TagKey,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDouble_PartitionKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagDouble_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDouble
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagDouble_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagDouble
    (
        ResourceType,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    WITH (DATA_COMPRESSION = PAGE)
END

---------------------

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_ExtendedQueryTagLong'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagLong ON dbo.ExtendedQueryTagLong
    (
        ResourceType,
        TagKey,
        TagValue,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    ) WITH (DROP_EXISTING = ON, ONLINE = ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagLong_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagLong_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagLong
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagLong_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagLong
    (
        ResourceType,
        TagKey,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END

    
IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagLong_PartitionKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagLong_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagLong
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagLong_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagLong
    (
        ResourceType,
        PartitionKey,
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
    WHERE   NAME = 'IXC_ExtendedQueryTagPersonName'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName ON dbo.ExtendedQueryTagPersonName
    (
        ResourceType,
        TagKey,
        TagValue,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    ) WITH (DROP_EXISTING = ON, ONLINE = ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagPersonName
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagPersonName_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagPersonName
    (
        ResourceType,
        TagKey,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
    
IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagPersonName_PartitionKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagPersonName_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagPersonName
END  

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagPersonName_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagPersonName
    (
        ResourceType,
        PartitionKey,
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
    WHERE   NAME = 'IXC_ExtendedQueryTagString'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagString ON dbo.ExtendedQueryTagString
    (
        ResourceType,
        TagKey,
        TagValue,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    ) WITH (DROP_EXISTING = ON, ONLINE = ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagString_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagString_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagString
END

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagString_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_TagKey_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagString
    (
        ResourceType,
        TagKey,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagString_PartitionKey_StudyKey_SeriesKey_InstanceKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    DROP INDEX IX_ExtendedQueryTagString_PartitionKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagString
END    
    
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ExtendedQueryTagString_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagString
    (
        ResourceType,
        PartitionKey,
        SopInstanceKey1,
        SopInstanceKey2,
        SopInstanceKey3
    )
    WITH (DATA_COMPRESSION = PAGE)
END

-- List of extended query tags that are supported for UPS-RS queries
IF NOT EXISTS (
SELECT 1 FROM  dbo.WorkitemQueryTag
)
BEGIN 
    BEGIN TRANSACTION

        -- Patient name
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00100010', 'PN')

        -- Patient ID
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00100020', 'LO')

        -- ReferencedRequestSequence.Accesionnumber
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00080050', 'SQ')

        -- ReferencedRequestSequence.Requested​Procedure​ID
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00401001', 'SQ')

        -- 	Scheduled​Procedure​Step​Start​Date​Time
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00404005', 'DT')

        -- 	ScheduledStationNameCodeSequence.CodeValue
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00404025.00080100', 'SQ')

        -- 	ScheduledStationClassCodeSequence.CodeValue
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00404026.00080100', 'SQ')

        -- 	Procedure​Step​State
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00741000', 'CS')

        -- 	Scheduled​Station​Geographic​Location​Code​Sequence.CodeValue
        INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
        VALUES (NEXT VALUE FOR TagKeySequence, '00404027.00080100', 'SQ')

    COMMIT TRANSACTION
END 
