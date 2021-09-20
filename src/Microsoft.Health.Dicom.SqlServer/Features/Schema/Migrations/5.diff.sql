/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
******************************************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION

IF NOT EXISTS (
    SELECT *
    FROM sys.tables
    WHERE name = 'Partition')
BEGIN
    CREATE TABLE dbo.Partition (
        PartitionKey            BIGINT                 NOT NULL, --PK
        PartitionId             VARCHAR(64)            NOT NULL    DEFAULT 'Microsoft.Default',
        --audit columns
        CreatedDate             DATETIME2(7)           NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)
END

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_Partition' AND object_id = OBJECT_ID('dbo.Partition'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_Partition ON dbo.Partition
    (
        PartitionKey
    )
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_Partition_PartitionKey_PartitionId' AND object_id = OBJECT_ID('dbo.Partition'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IXC_Partition_PartitionKey_PartitionId ON dbo.Partition
    (
        PartitionKey,
        PartitionId
    )
END
GO


/*************************************************************
    Sequence for generating sequential unique ids
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.sequences
    WHERE name = 'PartitionKeySequence')
BEGIN
    CREATE SEQUENCE dbo.PartitionKeySequence
        AS INT
        START WITH 1
        INCREMENT BY 1
        MINVALUE 1
        NO CYCLE
        CACHE 1000000
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Instance') AND name = 'PartitionKey')
BEGIN
    ALTER TABLE dbo.Instance
    ADD
        PartitionKey BIGINT DEFAULT 1 NOT NULL
END

IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid' AND object_id = OBJECT_ID('dbo.Instance'))
BEGIN
    DROP INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.Instance
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Instance_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid' AND object_id = OBJECT_ID('dbo.Instance'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.Instance
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    )
    INCLUDE
    (
        Status,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS(
    SELECT  *
    FROM    sys.index_columns ic
    JOIN    sys.indexes i
    ON      ic.object_id = i.object_id
            AND ic.index_id = i.index_id
    JOIN    sys.columns c
    ON      c.object_id = i.object_id
            AND c.column_id = ic.column_id
    WHERE   i.name = 'IX_Instance_SopInstanceUid_Status' 
            AND ic.object_id = OBJECT_ID('dbo.Instance')
            AND ic.is_included_column = 1
            AND c.name = 'PartitionKey'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_SopInstanceUid_Status ON dbo.Instance
    (
        SopInstanceUid,
        Status
    )
    INCLUDE
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING=ON, ONLINE=ON)
END
GO

IF NOT EXISTS(
    SELECT  *
    FROM    sys.index_columns ic
    JOIN    sys.indexes i
    ON      ic.object_id = i.object_id
            AND ic.index_id = i.index_id
    JOIN    sys.columns c
    ON      c.object_id = i.object_id
            AND c.column_id = ic.column_id
    WHERE   i.name = 'IX_Instance_SeriesKey_Status' 
            AND ic.object_id = OBJECT_ID('dbo.Instance')
            AND ic.is_included_column = 1
            AND c.name = 'PartitionKey'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_SeriesKey_Status ON dbo.Instance
    (
        SeriesKey,
        Status
    )
    INCLUDE
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING=ON, ONLINE=ON)
END
GO

IF NOT EXISTS(
    SELECT  *
    FROM    sys.index_columns ic
    JOIN    sys.indexes i
    ON      ic.object_id = i.object_id
            AND ic.index_id = i.index_id
    JOIN    sys.columns c
    ON      c.object_id = i.object_id
            AND c.column_id = ic.column_id
    WHERE   i.name = 'IX_Instance_StudyKey_Status' 
            AND ic.object_id = OBJECT_ID('dbo.Instance')
            AND ic.is_included_column = 1
            AND c.name = 'PartitionKey'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_StudyKey_Status ON dbo.Instance
    (
        StudyKey,
        Status
    )
    INCLUDE
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING=ON, ONLINE=ON)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Study') AND name = 'PartitionKey')
BEGIN
    ALTER TABLE dbo.Study
    ADD
        PartitionKey BIGINT DEFAULT 1 NOT NULL
END

IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Study_StudyInstanceUid' AND object_id = OBJECT_ID('dbo.Study'))
BEGIN
    DROP INDEX IX_Study_StudyInstanceUid ON dbo.Study
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Study_PartitionKey_StudyInstanceUid' AND object_id = OBJECT_ID('dbo.Study'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyInstanceUid ON dbo.Study
    (
        PartitionKey,
        StudyInstanceUid
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Series') AND name = 'PartitionKey')
BEGIN
    ALTER TABLE dbo.Series
    ADD
        PartitionKey BIGINT DEFAULT 1 NOT NULL
END

IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_Series' AND object_id = OBJECT_ID('dbo.Series'))
BEGIN
    DROP INDEX IXC_Series ON dbo.Series
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_PartitionKey_Series' AND object_id = OBJECT_ID('dbo.Series'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_PartitionKey_Series ON dbo.Series
    (
        PartitionKey,
        StudyKey,
        SeriesKey
    )
END
GO

IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Series_SeriesInstanceUid' AND object_id = OBJECT_ID('dbo.Series'))
BEGIN
    DROP INDEX IX_Series_SeriesInstanceUid ON dbo.Series
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Series_PartitionKey_SeriesInstanceUid' AND object_id = OBJECT_ID('dbo.Series'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Series_PartitionKey_SeriesInstanceUid ON dbo.Series
    (
        PartitionKey,
        SeriesInstanceUid
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DeletedInstance') AND name = 'PartitionId')
BEGIN
    ALTER TABLE dbo.DeletedInstance
    ADD
        PartitionId VARCHAR(64) DEFAULT 'Microsoft.Default' NOT NULL
END

IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_DeletedInstance' AND object_id = OBJECT_ID('dbo.DeletedInstance'))
BEGIN
    DROP INDEX IXC_DeletedInstance ON dbo.DeletedInstance
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_PartitionId_DeletedInstance' AND object_id = OBJECT_ID('dbo.DeletedInstance'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_PartitionId_DeletedInstance ON dbo.DeletedInstance
    (
        PartitionId,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        WaterMark
    )
END
GO

IF NOT EXISTS(
    SELECT  *
    FROM    sys.index_columns ic
    JOIN    sys.indexes i
    ON      ic.object_id = i.object_id
            AND ic.index_id = i.index_id
    JOIN    sys.columns c
    ON      c.object_id = i.object_id
            AND c.column_id = ic.column_id
    WHERE   i.name = 'IX_DeletedInstance_RetryCount_CleanupAfter' 
            AND ic.object_id = OBJECT_ID('dbo.DeletedInstance')
            AND ic.is_included_column = 1
            AND c.name = 'PartitionId'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeletedInstance_RetryCount_CleanupAfter ON dbo.DeletedInstance
    (
    RetryCount,
    CleanupAfter
    )
    INCLUDE
    (
        PartitionId,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING=ON, ONLINE=ON)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ChangeFeed') AND name = 'PartitionId')
BEGIN
    ALTER TABLE dbo.ChangeFeed
    ADD
        PartitionId VARCHAR(64) DEFAULT 'Microsoft.Default' NOT NULL
END

IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ChangeFeed_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid' AND object_id = OBJECT_ID('dbo.ChangeFeed'))
BEGIN
    DROP INDEX IX_ChangeFeed_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ChangeFeed_PartitionId_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid' AND object_id = OBJECT_ID('dbo.ChangeFeed'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChangeFeed_PartitionId_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed
    (
        PartitionId,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    )
END
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstance
--
-- DESCRIPTION
--     Adds a DICOM instance.
--
-- PARAMETERS
--     @partitionId
--         * The Partition Id
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
CREATE OR ALTER PROCEDURE dbo.AddInstance
    @partitionId                        VARCHAR(64)    = 'Microsoft.Default',
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
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
    @initialStatus                      TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @existingStatus TINYINT
    DECLARE @newWatermark BIGINT
    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT
    DECLARE @partitionKey BIGINT

    SELECT @partitionKey = PartitionKey
    FROM dbo.Partition WITH(UPDLOCK)
    WHERE PartitionId = @partitionId

    IF @@ROWCOUNT = 0
    BEGIN
        SET @partitionKey = NEXT VALUE FOR dbo.PartitionKeySequence

        INSERT INTO dbo.Partition
            (PartitionKey, PartitionId, CreatedDate)
        VALUES
            (@partitionKey, @partitionId, SYSUTCDATETIME())
    END

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
    WHERE StudyInstanceUid = @studyInstanceUid
    AND PartitionKey = @partitionKey

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
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate)

    -- Insert Extended Query Tags

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagString AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @stringExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Not merge on extended query tag which is being deleted.
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            -- When TagLevel is not Study, we should fill SeriesKey
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            -- When TagLevel is Instance, we should fill InstanceKey
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);
    END

    -- Long Key tags
    IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagLong AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @longExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);
    END

    -- Double Key tags
    IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagDouble AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @doubleExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagDateTime AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @dateTimeExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagPersonName AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @personNameExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);
    END

    SELECT @newWatermark

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatus
--
-- DESCRIPTION
--     Updates a DICOM instance status.
--
-- PARAMETERS
--     @partitionId
--         * The Partition ID.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark.
--     @status
--         * The new status to update to.
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatus
    @partitionId        VARCHAR(64) = 'Microsoft.Default',
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @status             TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @partitionKey BIGINT

    SELECT @partitionKey = PartitionKey
    FROM dbo.Partition WITH(UPDLOCK)
    WHERE PartitionId = @partitionId

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE StudyInstanceUid = @studyInstanceUid
    AND SeriesInstanceUid = @seriesInstanceUid
    AND SopInstanceUid = @sopInstanceUid
    AND Watermark = @watermark
    AND PartitionKey = @partitionKey

    IF @@ROWCOUNT = 0
    BEGIN
        -- The instance does not exist. Perhaps it was deleted?
        THROW 50404, 'Instance does not exist', 1;
    END

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Timestamp, Action, PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES
        (@currentDate, 0, @partitionId, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid
        AND PartitionId       = @partitionId

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     BeginAddInstance
--
-- DESCRIPTION
--     Begins the addition of a DICOM instance.
--
-- PARAMETERS
--     @partitionId
--         * The Partition ID.
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
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.BeginAddInstance
    @partitionId                        VARCHAR(64)   = 'Microsoft.Default',
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
    @manufacturerModelName              NVARCHAR(64) = NULL
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @existingStatus TINYINT
    DECLARE @newWatermark BIGINT
    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT
    DECLARE @partitionKey BIGINT

    SELECT @partitionKey = PartitionKey
    FROM dbo.Partition WITH(UPDLOCK)
    WHERE PartitionId = @partitionId

    IF @@ROWCOUNT = 0
    BEGIN
        SET @partitionKey = NEXT VALUE FOR dbo.PartitionKeySequence

        INSERT INTO dbo.Partition
            (PartitionKey, PartitionId, CreatedDate)
        VALUES
            (@partitionKey, @partitionId, SYSUTCDATETIME())
    END

    SELECT @existingStatus = Status
    FROM dbo.Instance WITH(HOLDLOCK)
    WHERE StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND PartitionKey = @partitionKey

    IF @@ROWCOUNT <> 0
        -- The instance already exists. Set the state = @existingStatus to indicate what state it is in.
        THROW 50409, 'Instance already exists', @existingStatus;

    -- The instance does not exist, insert it.
    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence
    SET @instanceKey = NEXT VALUE FOR dbo.InstanceKeySequence

    -- Insert Study
    SELECT @studyKey = StudyKey
    FROM dbo.Study WITH(HOLDLOCK)
    WHERE StudyInstanceUid = @studyInstanceUid
    AND PartitionKey = @partitionKey

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
    FROM dbo.Series WITH(HOLDLOCK)
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
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, 0, @currentDate, @currentDate)

    SELECT @newWatermark

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     EndAddInstance
--
-- DESCRIPTION
--     Completes the addition of a DICOM instance.
--
-- PARAMETERS
--     @partitionId
--         * The Partition ID.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark.
--     @maxTagKey
--         * Max ExtendedQueryTag key
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
CREATE OR ALTER PROCEDURE dbo.EndAddInstance
    @partitionId                        VARCHAR(64)   = 'Microsoft.Default',
    @studyInstanceUid  VARCHAR(64),
    @seriesInstanceUid VARCHAR(64),
    @sopInstanceUid    VARCHAR(64),
    @watermark         BIGINT,
    @maxTagKey         INT = NULL,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        -- This check ensures the client is not potentially missing 1 or more query tags that may need to be indexed.
        -- Note that if @maxTagKey is NULL, < will always return UNKNOWN.
        IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
            THROW 50409, 'Max extended query tag key does not match', 10

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
        DECLARE @partitionKey BIGINT

        SELECT @partitionKey = PartitionKey
        FROM dbo.Partition WITH(UPDLOCK)
        WHERE PartitionId = @partitionId

        UPDATE dbo.Instance
        SET Status = 1, LastStatusUpdatedDate = @currentDate
        WHERE StudyInstanceUid = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid = @sopInstanceUid
            AND Watermark = @watermark
            AND PartitionKey = @partitionKey

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exist', 1 -- The instance does not exist. Perhaps it was deleted?

        EXEC dbo.IndexInstance
            @watermark,
            @stringExtendedQueryTags,
            @longExtendedQueryTags,
            @doubleExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

        -- Insert to change feed.
        -- Currently this procedure is used only updating the status to created
        -- If that changes an if condition is needed.
        INSERT INTO dbo.ChangeFeed
            (Timestamp, Action, PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
        VALUES
            (@currentDate, 0, @partitionId, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

        -- Update existing instance currentWatermark to latest
        UPDATE dbo.ChangeFeed
        SET CurrentWatermark      = @watermark
        WHERE StudyInstanceUid    = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid    = @sopInstanceUid
            AND PartitionId       = @partitionId

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstance
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level
--
-- PARAMETERS
--     @partitionId
--         * The Partition ID.
--     @validStatus
--         * Filter criteria to search only valid instances
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstance (
    @partitionId        VARCHAR(64) = 'Microsoft.Default',
    @validStatus        TINYINT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @partitionKey BIGINT

    SELECT @partitionKey = PartitionKey
    FROM dbo.Partition WITH(UPDLOCK)
    WHERE PartitionId = @partitionId

    SELECT  StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            Watermark,
            @partitionId AS PartitionId
    FROM    dbo.Instance
    WHERE   StudyInstanceUid        = @studyInstanceUid
            AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
            AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
            AND Status              = @validStatus
            AND PartitionKey        = @partitionKey

END
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     GetInstancesByWatermarkRange
--
-- DESCRIPTION
--     Get instances by given watermark range.
--
-- PARAMETERS
--     @startWatermark
--         * The inclusive start watermark.
--     @endWatermark
--         * The inclusive end watermark.
--     @status
--         * The instance status.
-- RETURN VALUE
--     The instance identifiers.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetInstancesByWatermarkRange
    @startWatermark BIGINT,
    @endWatermark BIGINT,
    @status TINYINT
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark,
           p.PartitionId
    FROM dbo.Instance i
    JOIN dbo.Partition p
    ON p.PartitionKey = i.PartitionKey
    WHERE Watermark BETWEEN @startWatermark AND @endWatermark
          AND Status = @status
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstance
--
-- DESCRIPTION
--     Removes the specified instance(s) and places them in the DeletedInstance table for later removal
--
-- PARAMETERS
--     @cleanupAfter
--         * The date time offset that the instance can be cleaned up.
--     @createdStatus
--         * Status value representing the created state.
--     @partitionId
--         * The Partition ID.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteInstance
    @cleanupAfter       DATETIMEOFFSET(0),
    @createdStatus      TINYINT,
    @partitionId        VARCHAR(64) = 'Microsoft.Default',
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

    DECLARE @deletedInstances AS TABLE
        (PartitionId VARCHAR(64),
         StudyInstanceUid VARCHAR(64),
         SeriesInstanceUid VARCHAR(64),
         SopInstanceUid VARCHAR(64),
         Status TINYINT,
         Watermark BIGINT)

    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT
    DECLARE @deletedDate DATETIME2 = SYSUTCDATETIME()
    DECLARE @partitionKey BIGINT

    SELECT @partitionKey = PartitionKey
    FROM dbo.Partition WITH(UPDLOCK)
    WHERE PartitionId = @partitionId

    -- Get the study, series and instance PK
    SELECT  @studyKey = StudyKey,
    @seriesKey = CASE @seriesInstanceUid WHEN NULL THEN NULL ELSE SeriesKey END,
    @instanceKey = CASE @sopInstanceUid WHEN NULL THEN NULL ELSE InstanceKey END
    FROM    dbo.Instance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
    AND     PartitionKey = @partitionKey

    -- Delete the instance and insert the details into DeletedInstance and ChangeFeed
    DELETE  dbo.Instance
        OUTPUT @partitionId AS PartitionId, deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark
        INTO @deletedInstances
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
    AND     PartitionKey = @partitionKey

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
    INNER JOIN @deletedInstances as d
    ON XQTE.Watermark = d.Watermark

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
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagLong
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagDouble
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagDateTime
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagPersonName
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    INSERT INTO dbo.DeletedInstance
    (PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter)
    SELECT PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, @deletedDate, 0 , @cleanupAfter
    FROM @deletedInstances

    INSERT INTO dbo.ChangeFeed
    (TimeStamp, Action, PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT @deletedDate, 1, PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM @deletedInstances
    WHERE Status = @createdStatus

    UPDATE cf
    SET cf.CurrentWatermark = NULL
    FROM dbo.ChangeFeed cf WITH(FORCESEEK)
    JOIN @deletedInstances d
    ON cf.StudyInstanceUid = d.StudyInstanceUid
        AND cf.SeriesInstanceUid = d.SeriesInstanceUid
        AND cf.SopInstanceUid = d.SopInstanceUid
        AND cf.PartitionId = d.PartitionId

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance WITH(HOLDLOCK, UPDLOCK)
                    WHERE   StudyKey = @studyKey
                    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
                    AND     PartitionKey      = @partitionKey)
    BEGIN
        DELETE
        FROM    dbo.Series
        WHERE   Studykey = @studyKey
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     PartitionKey      = @partitionKey

        -- Deleting indexed series tags
        DELETE
        FROM    dbo.ExtendedQueryTagString
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    END

    -- If we've removing the series, see if it's the last for a study and if so, remove the study
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Series WITH(HOLDLOCK, UPDLOCK)
                    WHERE   StudyKey = @studyKey
                    AND     PartitionKey = @partitionKey)
    BEGIN
        DELETE
        FROM    dbo.Study
        WHERE   StudyKey = @studyKey

        -- Deleting indexed study tags
        DELETE
        FROM    dbo.ExtendedQueryTagString
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   StudyKey = @studyKey
    END

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstance
--
-- DESCRIPTION
--     Retrieves deleted instances where the cleanupAfter is less than the current date in and the retry count hasn't been exceeded
--
-- PARAMETERS
--     @count
--         * The number of entries to return
--     @maxRetries
--         * The maximum number of times to retry a cleanup
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstance
    @count          INT,
    @maxRetries     INT
AS
    SET NOCOUNT ON

    SELECT  TOP (@count) PartitionId, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM    dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteDeletedInstance
--
-- DESCRIPTION
--     Removes a deleted instance from the DeletedInstance table
--
-- PARAMETERS
--     @partitionId
--         * The Partition Id
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteDeletedInstance(
    @partitionId        VARCHAR(64) = 'Microsoft.Default',
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT
)
AS
    SET NOCOUNT ON

    DELETE
    FROM    dbo.DeletedInstance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = @seriesInstanceUid
    AND     SopInstanceUid = @sopInstanceUid
    AND     Watermark = @watermark
    AND     PartitionId = @partitionId
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     IncrementDeletedInstanceRetry
--
-- DESCRIPTION
--     Increments the retryCount of and retryAfter of a deleted instance
--
-- PARAMETERS
--     @partitionId
--         * The Partition Id
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
--     @cleanupAfter
--         * The next date time to attempt cleanup
--
-- RETURN VALUE
--     The retry count.
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IncrementDeletedInstanceRetry(
    @partitionId        VARCHAR(64) = 'Microsoft.Default',
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @cleanupAfter       DATETIMEOFFSET(0)
)
AS
    SET NOCOUNT ON

    DECLARE @retryCount INT

    UPDATE  dbo.DeletedInstance
    SET     @retryCount = RetryCount = RetryCount + 1,
            CleanupAfter = @cleanupAfter
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = @seriesInstanceUid
    AND     SopInstanceUid = @sopInstanceUid
    AND     Watermark = @watermark
    AND     PartitionId = @partitionId

    SELECT @retryCount
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeed
--
-- DESCRIPTION
--     Gets a stream of dicom changes (instance adds and deletes)
--
-- PARAMETERS
--     @limit
--         * Max rows to return
--     @offet
--         * Rows to skip
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeed (
    @limit      INT,
    @offset     BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  Sequence,
            Timestamp,
            Action,
            PartitionId,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed
    WHERE   Sequence BETWEEN @offset+1 AND @offset+@limit
    ORDER BY Sequence
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatest
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatest
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            PartitionId,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed
    ORDER BY Sequence DESC
END
GO

/*************************************************************
Store Procedure that checks if there are already existing record in instance table
**************************************************************/
CREATE OR ALTER PROCEDURE dbo.CheckIfInstancesExist
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT TOP 1 InstanceKey
    FROM dbo.Instance

    IF @@ROWCOUNT <> 0
       THROW 50410, 'Instances already exists', 1
END
GO
