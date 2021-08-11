SET XACT_ABORT ON

BEGIN TRANSACTION

IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ExtendedQueryTag') AND name = 'TagVersion')
BEGIN
    ALTER TABLE dbo.ExtendedQueryTag
    ADD TagVersion  ROWVERSION   NOT NULL
END

/*************************************************************
    Extended Query Tag Operation Table
    Stores the association between tags and their reindexing operation
    TagKey is the primary key and foreign key for the row in dbo.ExtendedQueryTag
    OperationId is the unique ID for the associated operation (like reindexing)
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagOperation')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagOperation (
        TagKey                  INT                  NOT NULL, --PK
        OperationId             uniqueidentifier     NOT NULL
    )
END

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_ExtendedQueryTagOperation' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagOperation'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagOperation ON dbo.ExtendedQueryTagOperation
    (
        TagKey
    )
END

IF NOT EXISTS (
    SELECT * 
    FROM sys.indexes 
    WHERE name='IX_ExtendedQueryTagOperation_OperationId' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagOperation'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagOperation_OperationId ON dbo.ExtendedQueryTagOperation
    (
        OperationId
    )
    INCLUDE
    (
        TagKey
    )
END
GO

/*************************************************************
    The user defined type for stored procedures that consume extended query tag keys
*************************************************************/
IF TYPE_ID(N'ExtendedQueryTagKeyTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.ExtendedQueryTagKeyTableType_1 AS TABLE
    (
        TagKey                     INT
    )
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetMaxInstanceWatermark
--
-- DESCRIPTION
--    Gets the maximum instance watermark, which could alternatively be thought of as an ETag for the state of Instance table
--
-- RETURN VALUE
--     The maximum instance watermark in the database
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetMaxInstanceWatermark
AS
    SET NOCOUNT ON

    SELECT MAX(Watermark) AS Watermark FROM dbo.Instance
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
CREATE OR ALTER PROCEDURE dbo.GetInstancesByWatermarkRange(
    @startWatermark BIGINT,
    @endWatermark BIGINT,
    @status TINYINT
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM dbo.Instance
    WHERE Watermark BETWEEN @startWatermark AND @endWatermark
          AND Status = @status
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagsByOperation
--
-- DESCRIPTION
--     Gets all extended query tags assigned to an operation.
--
-- PARAMETERS
--     @operationId
--         * The unique ID for the operation.
--
-- RETURN VALUE
--     The set of extended query tags assigned to the operation.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsByOperation (
    @operationId uniqueidentifier
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           TagVersion
    FROM dbo.ExtendedQueryTag AS XQT
    INNER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    WHERE OperationId = @operationId
END
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
--     The keys for the added tags.
/***************************************************************************************/
ALTER PROCEDURE dbo.AddExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @maxAllowedCount INT,
    @ready BIT = 0
)
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        -- Check if total count exceed @maxCount
        -- HOLDLOCK to prevent adding queryTags from other transactions at same time.
        IF (SELECT COUNT(*)
            FROM dbo.ExtendedQueryTag AS XQT WITH(HOLDLOCK)
            FULL OUTER JOIN @extendedQueryTags AS input ON XQT.TagPath = input.TagPath) > @maxAllowedCount
            THROW 50409, 'extended query tags exceed max allowed count', 1

        -- Check if tag with same path already exist
        -- Because the web client may fail between the addition of the tag and the starting of re-indexing operation,
        -- the stored procedure allows tags that are not assigned to an operation to be overwritten
        DECLARE @existingTags TABLE(TagKey INT, TagStatus TINYINT, OperationId uniqueidentifier NULL)

        INSERT INTO @existingTags
            (TagKey, TagStatus, OperationId)
        SELECT XQT.TagKey, TagStatus, OperationId
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @extendedQueryTags AS input ON input.TagPath = XQT.TagPath
        LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey

        IF EXISTS(SELECT 1 FROM @existingTags WHERE TagStatus <> 0 OR (TagStatus = 0 AND OperationId IS NOT NULL))
            THROW 50409, 'extended query tag(s) already exist', 2

        -- Delete any "pending" tags whose operation has yet to be assigned
        DELETE XQT
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @existingTags AS et
        ON XQT.TagKey = et.TagKey

        -- Add the new tags with the given status
        INSERT INTO dbo.ExtendedQueryTag
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        OUTPUT INSERTED.TagKey
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, @ready FROM @extendedQueryTags

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Reindex instance
--
-- DESCRIPTION
--    Reidex instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
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

/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.ReindexInstance
    @studyInstanceUid       VARCHAR(64),
    @seriesInstanceUid      VARCHAR(64),
    @sopInstanceUid         VARCHAR(64),
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
    SET NOCOUNT    ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT
        DECLARE @watermark BIGINT

        -- Add lock so that the instance won't be removed
        DECLARE @status TINYINT
        SELECT
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @watermark = Watermark,
            @status = Status
        FROM dbo.Instance WITH (HOLDLOCK) 
        WHERE StudyInstanceUid = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid = @sopInstanceUid

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance not exists', 1
        IF @status <> 1 -- Created
            THROW 50409, 'Instance is not been stored succssfully', 1

        -- Insert Extended Query Tags

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
                -- Only merge on extended query tag which is being adding.
                AND dbo.ExtendedQueryTag.TagStatus = 0
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
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
            MERGE INTO dbo.ExtendedQueryTagLong AS T
            USING 
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @longExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus = 0
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                 -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
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
            MERGE INTO dbo.ExtendedQueryTagDouble AS T
            USING
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @doubleExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus = 0
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
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
            MERGE INTO dbo.ExtendedQueryTagDateTime AS T
            USING
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @dateTimeExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus = 0
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                 -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
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
                AND dbo.ExtendedQueryTag.TagStatus = 0
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
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

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     AssignReindexingOperation
--
-- DESCRIPTION
--    Assigns the given operation ID to the set of extended query tags, if possible.
--
-- PARAMETERS
--     @extendedQueryTagKeys
--         * The list of extended query tag keys
--     @operationId
--         * The ID for the re-indexing operation
--     @returnIfCompleted
--         * Indicates whether completed tags should also be returned
--
-- RETURN VALUE
--     The subset of keys whose operation was successfully assigned.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AssignReindexingOperation (
    @extendedQueryTagKeys dbo.ExtendedQueryTagKeyTableType_1 READONLY,
    @operationId uniqueidentifier,
    @returnIfCompleted BIT = 0
)
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        MERGE INTO dbo.ExtendedQueryTagOperation WITH(HOLDLOCK) AS XQTO
        USING
        (
            SELECT input.TagKey
            FROM @extendedQueryTagKeys AS input
            INNER JOIN dbo.ExtendedQueryTag AS XQT WITH(HOLDLOCK) ON input.TagKey = XQT.TagKey
            WHERE TagStatus = 0
        ) AS tags
        ON XQTO.TagKey = tags.TagKey
        WHEN NOT MATCHED THEN
            INSERT (TagKey, OperationId)
            VALUES (tags.TagKey, @operationId);

        SELECT XQT.TagKey,
               TagPath,
               TagVR,
               TagPrivateCreator,
               TagLevel,
               TagStatus,
               TagVersion
        FROM @extendedQueryTagKeys AS input
        INNER JOIN dbo.ExtendedQueryTag AS XQT WITH(HOLDLOCK) ON input.TagKey = XQT.TagKey
        LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO WITH(HOLDLOCK) ON XQT.TagKey = XQTO.TagKey
        WHERE (@returnIfCompleted = 1 AND TagStatus = 1) OR (OperationId = @operationId AND TagStatus = 0)

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     CompleteReindexing
--
-- DESCRIPTION
--    Annotates each of the specified tags as "completed" by updating their tag statuses and
--    removing their association to the re-indexing operation
--
-- PARAMETERS
--     @extendedQueryTagKeys
--         * The list of extended query tag keys
--
-- RETURN VALUE
--     The keys for the completed tags
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.CompleteReindexing (
    @extendedQueryTagKeys dbo.ExtendedQueryTagKeyTableType_1 READONLY
)
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        -- Update the TagStatus of all rows to Completed (1)
        UPDATE XQT
        SET TagStatus = 1
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @extendedQueryTagKeys AS input ON XQT.TagKey = input.TagKey
        WHERE TagStatus = 0

        -- Delete their corresponding operations
        DELETE XQTO
        OUTPUT DELETED.TagKey
        FROM dbo.ExtendedQueryTagOperation AS XQTO
        INNER JOIN dbo.ExtendedQueryTag AS XQT ON XQTO.TagKey = XQT.TagKey
        INNER JOIN @extendedQueryTagKeys AS input ON XQT.TagKey = input.TagKey
        WHERE TagStatus = 1

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--      AddInstance
--
-- DESCRIPTION
--      Adds a DICOM instance.
--
-- PARAMETERS
--      @studyInstanceUid
--          * The study instance UID.
--      @seriesInstanceUid
--          * The series instance UID.
--      @sopInstanceUid
--          * The SOP instance UID.
--      @patientId
--          * The Id of the patient.
--      @patientName
--          * The name of the patient.
--      @referringPhysicianName
--          * The referring physician name.
--      @studyDate
--          * The study date.
--      @studyDescription
--          * The study description.
--      @accessionNumber
--          * The accession number associated for the study.
--      @modality
--          * The modality associated for the series.
--      @performedProcedureStepStartDate
--          * The date when the procedure for the series was performed.
--      @stringExtendedQueryTags
--          * String extended query tag data
--      @longExtendedQueryTags
--          * Long extended query tag data
--      @doubleExtendedQueryTags
--          * Double extended query tag data
--      @dateTimeExtendedQueryTags
--          * DateTime extended query tag data
--      @personNameExtendedQueryTags
--          * PersonName extended query tag data
--      @initialStatus
--          * Initial status 
--      @maxTagVersion
--          * Max ExtendedQueryTag version
-- RETURN VALUE
--     The watermark (version).
------------------------------------------------------------------------
ALTER PROCEDURE dbo.AddInstance
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
    @initialStatus                      TINYINT,
    @maxTagVersion           TIMESTAMP = NULL
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

    IF @maxTagVersion <> (SELECT MAX(TagVersion) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max Extended query tag version does not match', 10

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

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTag(s)
--
-- DESCRIPTION
--     Gets all extended query tags or given extended query tag by tag path
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag to retrieve.
/***************************************************************************************/
ALTER PROCEDURE dbo.GetExtendedQueryTag (
    @tagPath  VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TagKey,
            TagPath,
            TagVR,
            TagPrivateCreator,
            TagLevel,
            TagStatus,
            TagVersion
    FROM    dbo.ExtendedQueryTag
    WHERE   TagPath                 = ISNULL(@tagPath, TagPath)
END
GO

COMMIT TRANSACTION
GO
