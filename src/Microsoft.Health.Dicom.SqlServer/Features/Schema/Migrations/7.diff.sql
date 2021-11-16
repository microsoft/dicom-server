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
    ExtendedQueryTagDateTime Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagDateTime 
        ADD PartitionKey INT NOT NULL DEFAULT 1
END

/*************************************************************
    ExtendedQueryTagDouble Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagDouble 
        ADD PartitionKey INT NOT NULL DEFAULT 1
END

/*************************************************************
    ExtendedQueryTagLong Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagLong')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagLong 
        ADD PartitionKey INT NOT NULL DEFAULT 1
END

/*************************************************************
    ExtendedQueryTagPersonName Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagPersonName 
        ADD PartitionKey INT NOT NULL DEFAULT 1
END

/*************************************************************
    ExtendedQueryTagString Table
    Add PartitionKey column and update indexes.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionKey'
        AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagString')
)
BEGIN
    ALTER TABLE dbo.ExtendedQueryTagString 
        ADD PartitionKey INT NOT NULL DEFAULT 1
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance Core V7
--
-- FIRST SCHEMA VERSION
--     7
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
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCoreV7
    @partitionKey                                                                INT,
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
            AND T.PartitionKey = @partitionKey
            AND T.StudyKey = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, StudyKey, SeriesKey, InstanceKey, Watermark)
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
            AND T.PartitionKey = @partitionKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
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
            AND T.PartitionKey = @partitionKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
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
            AND T.PartitionKey = @partitionKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue, T.TagValueUtc = S.TagValueUtc
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, StudyKey, SeriesKey, InstanceKey, Watermark, TagValueUtc)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
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
            AND T.PartitionKey = @partitionKey
            AND T.StudyKey = @studyKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark
            );
    END
END
GO

/*************************************************************
    Stored procedure for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstanceV7
--
-- FIRST SCHEMA VERSION
--     7
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
CREATE OR ALTER PROCEDURE dbo.AddInstanceV7
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

        EXEC dbo.IIndexInstanceCoreV7
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

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstanceV7
--
-- FIRST SCHEMA VERSION
--     7
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
CREATE OR ALTER PROCEDURE dbo.DeleteInstanceV7
    @cleanupAfter       DATETIMEOFFSET(0),
    @createdStatus      TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
AS
BEGIN
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
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagLong
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagDouble
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagDateTime
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)
    AND     PartitionKey = @partitionKey

    DELETE
    FROM    dbo.ExtendedQueryTagPersonName
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)
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
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
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
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey
    END

    COMMIT TRANSACTION
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance V7
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
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV7
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

            EXEC dbo.IIndexInstanceCoreV7
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
        WHERE   NAME = 'IX_Instance_SeriesKey_Status'
            AND Object_id = OBJECT_ID('dbo.Instance')
    )
    BEGIN
        DROP INDEX IX_Instance_SeriesKey_Status ON dbo.Instance

        CREATE NONCLUSTERED INDEX IX_Instance_SeriesKey_Status_Watermark on dbo.Instance
        (
            SeriesKey,
            Status,
            Watermark
        )
        INCLUDE
        (
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_Instance_StudyKey_Status'
            AND Object_id = OBJECT_ID('dbo.Instance')
    )
    BEGIN
        DROP INDEX IX_Instance_StudyKey_Status ON dbo.Instance

        CREATE NONCLUSTERED INDEX IX_Instance_StudyKey_Status_Watermark on dbo.Instance
        (
            StudyKey,
            Status,
            Watermark
        )
        INCLUDE
        (
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_ExtendedQueryTagDateTime_TagKey_StudyKey_SeriesKey_InstanceKey'
            AND Object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime')
    )
    BEGIN
        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
        (
            TagKey,
            TagValue,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH
        (
            DROP_EXISTING = ON,
            ONLINE = ON
        )

        DROP INDEX IX_ExtendedQueryTagDateTime_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDateTime

        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_TagKey_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDateTime
        (
            TagKey,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagDateTime_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDateTime

        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDateTime
        (
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH (DATA_COMPRESSION = PAGE)

        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDouble ON dbo.ExtendedQueryTagDouble
        (
            TagKey,
            TagValue,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH
        (
            DROP_EXISTING = ON,
            ONLINE = ON
        )

        DROP INDEX IX_ExtendedQueryTagDouble_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDouble

        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDouble
        (
            TagKey,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagDouble_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagDouble

        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDouble
        (
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH (DATA_COMPRESSION = PAGE)

        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagLong ON dbo.ExtendedQueryTagLong
        (
            TagKey,
            TagValue,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH
        (
            DROP_EXISTING = ON,
            ONLINE = ON
        )

        DROP INDEX IX_ExtendedQueryTagLong_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagLong

        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagLong
        (
            TagKey,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagLong_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagLong

        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagLong_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagLong
        (
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH (DATA_COMPRESSION = PAGE)

        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName ON dbo.ExtendedQueryTagPersonName
        (
            TagKey,
            TagValue,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH
        (
            DROP_EXISTING = ON,
            ONLINE = ON
        )

        DROP INDEX IX_ExtendedQueryTagPersonName_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagPersonName

        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagPersonName
        (
            TagKey,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagPersonName_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagPersonName

        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagPersonName
        (
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH (DATA_COMPRESSION = PAGE)

        CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagString ON dbo.ExtendedQueryTagString
        (
            TagKey,
            TagValue,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH
        (
            DROP_EXISTING = ON,
            ONLINE = ON
        )

        DROP INDEX IX_ExtendedQueryTagString_TagKey_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagString

        CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagString_PartitionKey_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagString
        (
            TagKey,
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        INCLUDE
        (
            Watermark
        )
        WITH (DATA_COMPRESSION = PAGE)

        DROP INDEX IX_ExtendedQueryTagString_StudyKey_SeriesKey_InstanceKey ON dbo.ExtendedQueryTagString

        CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagString
        (
            PartitionKey,
            StudyKey,
            SeriesKey,
            InstanceKey
        )
        WITH (DATA_COMPRESSION = PAGE)

    END


    COMMIT TRANSACTION

END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;
END CATCH
