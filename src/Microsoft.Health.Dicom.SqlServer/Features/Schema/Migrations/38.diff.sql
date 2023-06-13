SET XACT_ABORT ON

BEGIN TRANSACTION
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
--     @initialStatus
--         * Initial status of the row
--     @transferSyntaxUid
--         * Instance transfer syntax UID

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
    @initialStatus                      TINYINT,
    @transferSyntaxUid                  VARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON

    -- We turn off XACT_ABORT so that we can rollback and retry the INSERT/UPDATE into the study table on failure
    SET XACT_ABORT OFF

    -- The transaction is wrapped in a try...catch block in case the INSERT into the study table fails
    BEGIN TRY

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
            -- If we fail to INSERT, we instead must UPDATE the newly added value
            SELECT @studyKey = StudyKey
            FROM dbo.Study WITH(UPDLOCK)
            WHERE PartitionKey = @partitionKey
                AND StudyInstanceUid = @studyInstanceUid

            IF @@ROWCOUNT = 0
            BEGIN TRY

                SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

                INSERT INTO dbo.Study
                    (PartitionKey, StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
                VALUES
                    (@partitionKey, @studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)

            END TRY
            BEGIN CATCH

                 -- While we could obtain a HOLDLOCK on the table, we optimistically obtain an UPDLOCK instead to avoid the range lock on the study table
                IF ERROR_NUMBER() = 2601
                BEGIN

                    SELECT @studyKey = StudyKey
                    FROM dbo.Study WITH(UPDLOCK)
                    WHERE PartitionKey = @partitionKey
                        AND StudyInstanceUid = @studyInstanceUid

                    -- Latest wins
                    UPDATE dbo.Study
                    SET PatientId = ISNULL(@patientId, PatientId),
                        PatientName = ISNULL(@patientName, PatientName),
                        PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate),
                        ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName),
                        StudyDate = ISNULL(@studyDate, StudyDate),
                        StudyDescription = ISNULL(@studyDescription, StudyDescription),
                        AccessionNumber = ISNULL(@accessionNumber, AccessionNumber)
                    WHERE PartitionKey = @partitionKey
                      AND StudyKey = @studyKey

                END
                ELSE
                    THROW

            END CATCH
            ELSE
            BEGIN
                -- Latest wins
                UPDATE dbo.Study
                SET PatientId = ISNULL(@patientId, PatientId),
                    PatientName = ISNULL(@patientName, PatientName),
                    PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate),
                    ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName),
                    StudyDate = ISNULL(@studyDate, StudyDate),
                    StudyDescription = ISNULL(@studyDescription, StudyDescription),
                    AccessionNumber = ISNULL(@accessionNumber, AccessionNumber)
                WHERE PartitionKey = @partitionKey
                    AND StudyKey = @studyKey
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
                SET Modality = ISNULL(@modality, Modality),
                    PerformedProcedureStepStartDate = ISNULL(@performedProcedureStepStartDate, PerformedProcedureStepStartDate),
                    ManufacturerModelName = ISNULL(@manufacturerModelName, ManufacturerModelName)
                WHERE SeriesKey = @seriesKey
                AND StudyKey = @studyKey
                AND PartitionKey = @partitionKey
            END

            -- Insert Instance
            INSERT INTO dbo.Instance
                (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate, TransferSyntaxUid)
            VALUES
                (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate, @transferSyntaxUid)

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

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW

    END CATCH
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
--
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
    -- Note that it is the responsibility of the callers to lock the appropriate indexes to prevent incorrect updates.
    DECLARE @resourceType TINYINT = 0

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagString AS T
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
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
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
        MERGE INTO dbo.ExtendedQueryTagLong AS T
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
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
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
        MERGE INTO dbo.ExtendedQueryTagDouble AS T
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
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
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
        MERGE INTO dbo.ExtendedQueryTagDateTime AS T
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
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
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
        MERGE INTO dbo.ExtendedQueryTagPersonName AS T
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
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
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
COMMIT TRANSACTION
