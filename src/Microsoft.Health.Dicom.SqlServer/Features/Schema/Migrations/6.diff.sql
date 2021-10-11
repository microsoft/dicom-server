/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
******************************************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION

IF NOT EXISTS (
    SELECT *
    FROM sys.columns c
    INNER JOIN sys.index_columns ic
    ON c.object_id = ic.object_id AND ic.column_id = c.column_id
    INNER JOIN sys.indexes i
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.name='IX_Instance_Watermark' AND i.object_id = OBJECT_ID('dbo.Instance') and c.name = 'Status')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_Watermark on dbo.Instance
    (
        Watermark
    )
    INCLUDE
    (
        Status
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING = ON, ONLINE = ON)
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.columns c
    INNER JOIN sys.index_columns ic
    ON c.object_id = ic.object_id AND ic.column_id = c.column_id
    INNER JOIN sys.indexes i
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.name='IXC_ExtendedQueryTagDateTime' AND i.object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime') and c.name = 'TagValueUtc')
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
    (
        TagKey,
        TagValue,
        TagValueUtc,
        StudyKey,
        SeriesKey,
        InstanceKey
    ) WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING = ON, ONLINE = ON)
END
GO

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
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstanceV2
--
-- DESCRIPTION
--     Adds a DICOM instance.
--
-- PARAMETERS
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
CREATE OR ALTER PROCEDURE dbo.AddInstanceV2
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
    SET NOCOUNT    ON
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
    WHERE StudyInstanceUid = @studyInstanceUid
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

    IF @@ROWCOUNT = 0
    BEGIN
        SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

        INSERT INTO dbo.Study
            (StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
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

    IF @@ROWCOUNT = 0
    BEGIN
        SET @seriesKey = NEXT VALUE FOR dbo.SeriesKeySequence

        INSERT INTO dbo.Series
            (StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate, ManufacturerModelName)
        VALUES
            (@studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate, @manufacturerModelName)
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
        (StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate)

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
--    Index instance V2
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
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV2
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
--     UpdateInstanceStatus
--
-- DESCRIPTION
--     Updates a DICOM instance status.
--
-- PARAMETERS
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
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatus
    @studyInstanceUid  VARCHAR(64),
    @seriesInstanceUid VARCHAR(64),
    @sopInstanceUid    VARCHAR(64),
    @watermark         BIGINT,
    @status            TINYINT,
    @maxTagKey         INT = NULL
AS
BEGIN
    SET NOCOUNT    ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

    -- This check ensures the client is not potentially missing 1 or more query tags that may need to be indexed.
    -- Note that if @maxTagKey is NULL, < will always return UNKNOWN.
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE StudyInstanceUid = @studyInstanceUid
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
        (Timestamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES
        (@currentDate, 0, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION
GO
