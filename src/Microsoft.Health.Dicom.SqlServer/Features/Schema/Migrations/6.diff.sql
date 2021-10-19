/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md

This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - After the second transaction, there's an update to a full-text index which cannot be in a transaction.
******************************************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    Partition Sequence
    Create sequence for partition key, with default value 1 reserved.
**************************************************************/
IF NOT EXISTS
(
    SELECT * FROM sys.sequences
    WHERE Name = 'PartitionKeySequence'
)
BEGIN
    CREATE SEQUENCE dbo.PartitionKeySequence
    AS INT
    START WITH 2    -- skipping the default partition
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000
END

/*************************************************************
    Partition Table
    Create table containing data partitions for light-weight multitenancy.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.tables
    WHERE   Name = 'Partition'
)
BEGIN
    CREATE TABLE dbo.Partition (
        PartitionKey                INT             NOT NULL, --PK  System-generated sequence
        PartitionName               VARCHAR(64)     NOT NULL, --Client-generated unique name. Length allows GUID or UID.
        -- audit columns
        CreatedDate                 DATETIME2(7)    NOT NULL
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_Partition ON dbo.Partition
    (
        PartitionKey
    )

    -- Used in partition lookup in AddInstance or GetPartition
    CREATE UNIQUE NONCLUSTERED INDEX IX_Partition_PartitionName ON dbo.Partition
    (
        PartitionName
    )
    INCLUDE
    (
        PartitionKey
    )

    -- Add default partition values
    INSERT INTO dbo.Partition
        (PartitionKey, PartitionName, CreatedDate)
    VALUES
        (1, 'Microsoft.Default', SYSUTCDATETIME())
END

/*************************************************************
    Study Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.Study')
)
BEGIN
    ALTER TABLE dbo.Study
        ADD PartitionKey INT NOT NULL DEFAULT 1
END

/*************************************************************
    Series Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.Series')
)
BEGIN
    ALTER TABLE dbo.Series
        ADD PartitionKey INT NOT NULL DEFAULT 1
END

/*************************************************************
    Instance Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    ALTER TABLE dbo.Instance
        ADD PartitionKey    INT             NOT NULL DEFAULT 1
END

/*************************************************************
    ChangeFeed Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.ChangeFeed')
)
BEGIN
    ALTER TABLE dbo.ChangeFeed
        ADD PartitionKey    INT             NOT NULL DEFAULT 1
END

/*************************************************************
    DeletedInstance Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    ALTER TABLE dbo.DeletedInstance
        ADD PartitionKey    INT             NOT NULL DEFAULT 1
END

/*************************************************************
    ExtendedQueryTagDateTime Table
    Add new index for DeleteInstance.
**************************************************************/
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagDateTime_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDateTime
    (
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    ExtendedQueryTagDouble Table
    Add new index for DeleteInstance.
**************************************************************/
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagDouble_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDouble
    (
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    ExtendedQueryTagError Table
    Add new index for DeleteInstance and make IX_ExtendedQueryTagError_CreatedTime_Watermark_TagKey unique.
**************************************************************/
IF 1 != ISNULL(
(
    SELECT is_unique
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagError_CreatedTime_Watermark_TagKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagError')
), 0)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagError_CreatedTime_Watermark_TagKey ON dbo.ExtendedQueryTagError
    (
        CreatedTime,
        Watermark,
        TagKey
    )
    INCLUDE
    (
        ErrorCode
    ) WITH (DROP_EXISTING = ON, ONLINE = ON)
END

IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagError_Watermark' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagError')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagError_Watermark ON dbo.ExtendedQueryTagError
    (
        Watermark
    )
END

/*************************************************************
    ExtendedQueryTagLong Table
    Add new index for DeleteInstance.
**************************************************************/
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagLong_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagLong
    (
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    ExtendedQueryTagPersonName Table
    Add new index for DeleteInstance.
**************************************************************/
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagPersonName_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagPersonName
    (
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END

/*************************************************************
    ExtendedQueryTagString Table
    Add new index for DeleteInstance.
**************************************************************/
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagString_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagString
    (
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

/*************************************************************
    Stored procedures that are no longer necessary
*************************************************************/
DROP PROCEDURE IF EXISTS dbo.BeginAddInstance, dbo.EndAddInstance
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance Core
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
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
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCore
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
            -- Only merge on extended query tag which is being adding.
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @studyKey,
                -- When TagLevel is not Study, we should fill SeriesKey
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                -- When TagLevel is Instance, we should fill InstanceKey
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark
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
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark
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
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark
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
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue, T.TagValueUtc = S.TagValueUtc
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark, TagValueUtc)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                S.TagValueUtc
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
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark
            );
    END
END
GO

/*************************************************************
    Stored procedure for adding a partition.
**************************************************************/
--
-- STORED PROCEDURE
--     AddPartition
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Adds a partition.
--
-- PARAMETERS
--     @partitionName
--         * The client-provided data partition name.
--
-- RETURN VALUE
--     The partition.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddPartition
    @partitionName  VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @createdDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @partitionKey INT

    -- Insert Partition
    SET @partitionKey = NEXT VALUE FOR dbo.PartitionKeySequence

    INSERT INTO dbo.Partition
        (PartitionKey, PartitionName, CreatedDate)
    VALUES
        (@partitionKey, @partitionName, @createdDate)

    SELECT @partitionKey, @partitionName, @createdDate

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

        EXEC dbo.IIndexInstanceCore
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

        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT

        -- Add lock so that the instance cannot be removed
        DECLARE @status TINYINT
        SELECT
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

        BEGIN TRY

            EXEC dbo.IIndexInstanceCore
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
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatusV6
--
-- DESCRIPTION
--     Updates a DICOM instance status, which allows for consistency during indexing.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
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
--     @maxTagKey
--         * Optional max ExtendedQueryTag key
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV6
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @status             TINYINT,
    @maxTagKey          INT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    -- This check ensures the client is not potentially missing 1 or more query tags that may need to be indexed.
    -- Note that if @maxTagKey is NULL, < will always return UNKNOWN.
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND Watermark = @watermark

    -- The instance does not exist. Perhaps it was deleted?
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Timestamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES
        (@currentDate, 0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid

    COMMIT TRANSACTION
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceBatches
--
-- DESCRIPTION
--     Divides up the instances into a configurable number of batches.
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @batchCount
--         * The desired number of batches. Actual number may be smaller.
--     @status
--         * The instance status.
--     @maxWatermark
--         * The optional inclusive maximum watermark.
--
-- RETURN VALUE
--     The batches as defined by their inclusive minimum and maximum values.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceBatches
    @batchSize INT,
    @batchCount INT,
    @status TINYINT,
    @maxWatermark BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        MIN(Watermark) AS MinWatermark,
        MAX(Watermark) AS MaxWatermark
    FROM
    (
        SELECT TOP (@batchSize * @batchCount)
            Watermark,
            (ROW_NUMBER() OVER(ORDER BY Watermark DESC) - 1) / @batchSize AS Batch
        FROM dbo.Instance
        WHERE Watermark <= ISNULL(@maxWatermark, Watermark) AND Status = @status
    ) AS I
    GROUP BY Batch
    ORDER BY Batch ASC
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteDeletedInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Removes a deleted instance from the DeletedInstance table
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteDeletedInstanceV6(
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT
)
AS
BEGIN
    SET NOCOUNT ON

    DELETE
    FROM    dbo.DeletedInstance
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = @seriesInstanceUid
        AND     SopInstanceUid = @sopInstanceUid
        AND     Watermark = @watermark
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
--     GetChangeFeedLatestV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV6
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    ORDER BY Sequence DESC
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedV6
--
-- FIRST SCHEMA VERSION
--     6
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
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV6 (
    @limit      INT,
    @offset     BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    WHERE   Sequence BETWEEN @offset+1 AND @offset+@limit
    ORDER BY Sequence
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagErrorsV6
--
-- FIRST SCHEMA VERSION
--     6
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
    SELECT @tagKey = TagKey
    FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)
    WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

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
--     GetInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceV6 (
    @validStatus        TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON


    SELECT  StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            Watermark
    FROM    dbo.Instance
    WHERE   PartitionKey            = @partitionKey
            AND StudyInstanceUid    = @studyInstanceUid
            AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
            AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
            AND Status              = @validStatus

END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceBatches
--
-- DESCRIPTION
--     Divides up the instances into a configurable number of batches.
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @batchCount
--         * The desired number of batches. Actual number may be smaller.
--     @status
--         * The instance status.
--     @maxWatermark
--         * The optional inclusive maximum watermark.
--
-- RETURN VALUE
--     The batches as defined by their inclusive minimum and maximum values.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceBatches
    @batchSize INT,
    @batchCount INT,
    @status TINYINT,
    @maxWatermark BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        MIN(Watermark) AS MinWatermark,
        MAX(Watermark) AS MaxWatermark
    FROM
    (
        SELECT TOP (@batchSize * @batchCount)
            Watermark,
            (ROW_NUMBER() OVER(ORDER BY Watermark DESC) - 1) / @batchSize AS Batch
        FROM dbo.Instance
        WHERE Watermark <= ISNULL(@maxWatermark, Watermark) AND Status = @status
    ) AS I
    GROUP BY Batch
    ORDER BY Batch ASC
END
GO

/**************************************************************/
--
-- STORED PROCEDURE
--     GetInstancesByWatermarkRangeV6
--
-- FIRST SCHEMA VERSION
--     6
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
CREATE OR ALTER PROCEDURE dbo.GetInstancesByWatermarkRangeV6
    @startWatermark BIGINT,
    @endWatermark BIGINT,
    @status TINYINT
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM dbo.Instance
    WHERE Watermark BETWEEN @startWatermark AND @endWatermark
          AND Status = @status
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetPartitions
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets all data partitions
--
-- PARAMETERS
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetPartitions AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  PartitionKey,
            PartitionName,
            CreatedDate
    FROM    dbo.Partition
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetPartition
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets the partition for the specified name
--
-- PARAMETERS
--     @partitionName
--         * Client provided partition name
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetPartition (
    @partitionName   VARCHAR(64)
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  PartitionKey,
            PartitionName,
            CreatedDate
    FROM    dbo.Partition
    WHERE PartitionName = @partitionName
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     IncrementDeletedInstanceRetryV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Increments the retryCount of and retryAfter of a deleted instance
--
-- PARAMETERS
--     @partitionName
--         * The client-provided data partition name.
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
CREATE OR ALTER PROCEDURE dbo.IncrementDeletedInstanceRetryV6(
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @cleanupAfter       DATETIMEOFFSET(0)
)
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @retryCount INT

    UPDATE  dbo.DeletedInstance
    SET     @retryCount = RetryCount = RetryCount + 1,
            CleanupAfter = @cleanupAfter
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = @seriesInstanceUid
        AND     SopInstanceUid = @sopInstanceUid
        AND     Watermark = @watermark

    SELECT @retryCount
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
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
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV6
    @count          INT,
    @maxRetries     INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT  TOP (@count) PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM    dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
END
GO

COMMIT TRANSACTION

/*************************************************************
Drop Study fulltext index outside transaction
**************************************************************/

IF EXISTS (SELECT *
               FROM   sys.fulltext_indexes
               WHERE  object_id = object_id('dbo.Study'))
BEGIN
    -- This index uses IXC_Study as it's unique index, so must be dropped first.
    -- We'll restore the fulltext index with a new unique index after this transaction.
    DROP FULLTEXT INDEX ON dbo.Study    
END
GO


SET XACT_ABORT ON
BEGIN TRANSACTION

BEGIN TRY           -- wrapping the contents of this transaction in try/catch because errors on index
                    -- operations won't rollback unless caught and re-thrown

/*******************        Study       **********************/

-- This pattern is followed for several tables below. We are recreating the clustered index, adding
-- PartitionKey and then recreating all non-clustered indexes, dropping and creating
-- with a new name if we are adding new keys, or recreating to properly include the new clustered
-- index keys.
-- We first check if one of the old non-clustered indexes that should be dropped is present. If so,
-- we assume that this portion of the transaction has not successfully completed so far, and proceed.
IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Study_StudyInstanceUid'  -- A non-clustered index that would be dropped if previously run
        AND Object_id = OBJECT_ID('dbo.Study')
)
BEGIN
    -- recreate the clustered index
    CREATE UNIQUE CLUSTERED INDEX IXC_Study ON dbo.Study
    (
        PartitionKey,
        StudyKey
    )
    WITH
    (
        DROP_EXISTING = ON,
        ONLINE = ON
    )

    -- Used as the unique index for full-text index - must be a unique, non-nullable, single-column index
    CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyKey ON dbo.Study
    (
        StudyKey
    ) WITH (DATA_COMPRESSION = PAGE)

    -- Drop an existing index and create with a new name to reflect new keys
    DROP INDEX IX_Study_StudyInstanceUid ON dbo.Study
    -- Used in AddInstance; we include PartitionKey second because we assume conflicting StudyInstanceUid will be rare
    CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyInstanceUid_PartitionKey ON dbo.Study
    (
        StudyInstanceUid,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_PatientId ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_PatientId_PartitionKey ON dbo.Study
    (
        PatientId,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_PatientName ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_PatientName_PartitionKey ON dbo.Study
    (
        PatientName,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_ReferringPhysicianName ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_ReferringPhysicianName_PartitionKey ON dbo.Study
    (
        ReferringPhysicianName,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_StudyDate ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_StudyDate_PartitionKey ON dbo.Study
    (
        StudyDate,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_StudyDescription ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_StudyDescription_PartitionKey ON dbo.Study
    (
        StudyDescription,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_AccessionNumber ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_AccessionNumber_PartitionKey ON dbo.Study
    (
        AccessionNumber,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Study_PatientBirthDate ON dbo.Study
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Study_PatientBirthDate_PartitionKey ON dbo.Study
    (
        PatientBirthDate,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

END

/*******************        Series       **********************/
IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Series_SeriesInstanceUid'
        AND Object_id = OBJECT_ID('dbo.Series')
)
BEGIN
    -- Ordering studies by partition, study, and series key for partition-specific retrieval
    CREATE UNIQUE CLUSTERED INDEX IXC_Series ON dbo.Series
    (
        PartitionKey,
        StudyKey,
        SeriesKey
    )
    WITH
    (
        DROP_EXISTING = ON,
        ONLINE = ON
    )

    CREATE UNIQUE NONCLUSTERED INDEX IX_Series_SeriesKey ON dbo.Series
    (
        SeriesKey
    )
    WITH
    (
        DATA_COMPRESSION = PAGE,
        DROP_EXISTING = ON,
        ONLINE = ON
    )

    DROP INDEX IX_Series_SeriesInstanceUid ON dbo.Series
    -- Used in QIDO when querying at the study level; we place PartitionKey second because we assume conflicting SeriesInstanceUid will be rare
    CREATE UNIQUE NONCLUSTERED INDEX IX_Series_SeriesInstanceUid_PartitionKey ON dbo.Series
    (
        SeriesInstanceUid,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Series_Modality ON dbo.Series
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Series_Modality_PartitionKey ON dbo.Series
    (
        Modality,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey,
        SeriesKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Series_PerformedProcedureStepStartDate ON dbo.Series
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Series_PerformedProcedureStepStartDate_PartitionKey ON dbo.Series
    (
        PerformedProcedureStepStartDate,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey,
        SeriesKey
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Series_ManufacturerModelName ON dbo.Series
    -- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Series_ManufacturerModelName_PartitionKey ON dbo.Series
    (
        ManufacturerModelName,
        PartitionKey
    )
    INCLUDE
    (
        StudyKey,
        SeriesKey
    )
    WITH (DATA_COMPRESSION = PAGE)

END

/*******************        Instance       **********************/

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN

    --Filter indexes
    DROP INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.Instance
    -- Used in AddInstance, DeleteInstance, DeleteDeletedInstance, QIDO, putting PartitionKey last allows us to query across partitions in the future.
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid_PartitionKey on dbo.Instance
    (
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        PartitionKey
    )
    INCLUDE
    (
        Status,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Instance_StudyInstanceUid_Status ON dbo.Instance
    -- Used in WADO and QIDO, putting PartitionKey last allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_Status_PartitionKey on dbo.Instance
    (
        StudyInstanceUid,
        Status,
        PartitionKey    
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status ON dbo.Instance
    -- Used in WADO and QIDO, putting PartitionKey last allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status_PartitionKey on dbo.Instance
    (
        StudyInstanceUid,
        SeriesInstanceUid,
        Status,
        PartitionKey    
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Instance_SopInstanceUid_Status ON dbo.Instance
    -- Used in WADO and QIDO, putting PartitionKey last allows us to query across partitions in the future.
    CREATE NONCLUSTERED INDEX IX_Instance_SopInstanceUid_Status_PartitionKey on dbo.Instance
    (
        SopInstanceUid,
        Status,
        PartitionKey    
    )
    INCLUDE
    (
        StudyInstanceUid,
        SeriesInstanceUid,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)

    DROP INDEX IX_Instance_Watermark ON dbo.Instance
    -- Used in GetInstancesByWatermarkRange
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_Watermark_Status on dbo.Instance
    (
        Watermark,
        Status
    )
    INCLUDE
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    )
    WITH (DATA_COMPRESSION = PAGE)

END

/*******************       ChangeFeed      **********************/
IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ChangeFeed_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid'
        AND Object_id = OBJECT_ID('dbo.ChangeFeed')
)
BEGIN
    DROP INDEX IX_ChangeFeed_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed

    CREATE NONCLUSTERED INDEX IX_ChangeFeed_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    ) WITH (DATA_COMPRESSION = PAGE)
END

/***************        DeletedInstance       *******************/
IF NOT EXISTS  (SELECT  *
    FROM    sys.index_columns ic
    JOIN    sys.indexes i
    ON      ic.object_id = i.object_id
            AND ic.index_id = i.index_id
    JOIN    sys.columns c
    ON      c.object_id = i.object_id
            AND c.column_id = ic.column_id
    WHERE   i.name = 'IXC_DeletedInstance'
            AND ic.object_id = OBJECT_ID('dbo.DeletedInstance')
            AND ic.is_included_column = 0
            AND c.name = 'PartitionKey')
BEGIN
   CREATE UNIQUE CLUSTERED INDEX IXC_DeletedInstance ON dbo.DeletedInstance
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark
    ) 
    WITH
    (
        DROP_EXISTING = ON,
        ONLINE = ON
    )

    CREATE NONCLUSTERED INDEX IX_DeletedInstance_RetryCount_CleanupAfter ON dbo.DeletedInstance
    (
        RetryCount,
        CleanupAfter
    )
    INCLUDE
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark
    )
    WITH
    (
        DATA_COMPRESSION = PAGE,
        DROP_EXISTING = ON,
        ONLINE = ON
    )

END

COMMIT TRANSACTION

END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;
END CATCH

/*************************************************************
    Indexes
**************************************************************/
SET XACT_ABORT ON
BEGIN TRANSACTION

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagDateTime_TagKey_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDateTime
    (
        TagKey,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagDouble_TagKey_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDouble
    (
        TagKey,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagLong_TagKey_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagLong'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagLong
    (
        TagKey,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagPersonName_TagKey_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagPersonName
    (
        TagKey,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF 'PAGE' != (
    SELECT data_compression_desc
    FROM sys.partitions p
    INNER JOIN sys.indexes i
    ON p.object_id = i.object_id AND p.index_id = i.index_id
    WHERE i.name='IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey' AND i.object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName'))
BEGIN
    ALTER INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey ON dbo.ExtendedQueryTagPersonName
    REBUILD
    WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_ExtendedQueryTagString_TagKey_StudyKey_SeriesKey_InstanceKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagString'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagString_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagString
    (
        TagKey,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
    INCLUDE
    (
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

COMMIT TRANSACTION

/*************************************************************
Full text catalog and index creation outside transaction
**************************************************************/

IF NOT EXISTS (SELECT *
               FROM   sys.fulltext_indexes
               WHERE  object_id = object_id('dbo.Study'))
BEGIN
    CREATE FULLTEXT INDEX ON Study(PatientNameWords, ReferringPhysicianNameWords LANGUAGE 1033)
    KEY INDEX IX_Study_StudyKey
    WITH STOPLIST = OFF;
END
GO
