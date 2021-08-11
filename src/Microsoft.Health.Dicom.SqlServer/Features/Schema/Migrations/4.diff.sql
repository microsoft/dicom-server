SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    Extended Query Tag Errors Table
    Stores errors from Extended Query Tag operations
    TagKey and Watermark is Primary Key
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagError')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagError (
        TagKey                  INT             NOT NULL, --FK
        ErrorMessage            NVARCHAR(128)   NOT NULL,
        Watermark               BIGINT          NOT NULL,
        CreatedTime             DATETIME2(7)    NOT NULL,
    )
END
IF NOT EXISTS (
    SELECT * 
    FROM sys.indexes 
    WHERE name='IXC_ExtendedQueryTagError' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagError'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagError ON dbo.ExtendedQueryTagError
    (
        TagKey,
        Watermark
    )
END
GO

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
        OperationId             VARCHAR(32)          NOT NULL
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
    @operationId VARCHAR(32)
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
           TagStatus
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
        DECLARE @existingTags TABLE(TagKey INT, TagStatus TINYINT, OperationId VARCHAR(32) NULL)

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
            THROW 50404, 'Instance does not exists', 1
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
--     GetExtendedQueryTagErrors
--
-- DESCRIPTION
--     Gets the extended query tag errors by tag path.
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag for which we retrieve error(s).
--
-- RETURN VALUE
--     The tag error fields and the corresponding instance UIDs.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrors (@tagPath VARCHAR(64))
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
        ErrorMessage,
        CreatedTime,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    FROM dbo.ExtendedQueryTagError AS XQTE
    INNER JOIN dbo.Instance AS I
    ON XQTE.Watermark = I.Watermark
    WHERE XQTE.TagKey = @tagKey
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     AddExtendedQueryTagError
--
-- DESCRIPTION
--    Adds an Extended Query Tag Error or Updates it if exists.
--
-- PARAMETERS
--     @tagKey
--         * The related extended query tag's key
--     @errorMessage
--         * The error message
--     @watermark
--         * The watermark
--
-- RETURN VALUE
--     The tag key of the error added.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTagError (
    @tagKey INT,
    @errorMessage NVARCHAR(128),
    @watermark BIGINT
)
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

        --Check if instance with given watermark and Created status.
        IF NOT EXISTS (SELECT * FROM dbo.Instance WITH (UPDLOCK) WHERE Watermark = @watermark AND Status = 1)
            THROW 50404, 'Instance does not exist or has not been created.', 1;

        --Check if tag exists and in Adding status.
        IF NOT EXISTS (SELECT * FROM dbo.ExtendedQueryTag WITH (HOLDLOCK) WHERE TagKey = @tagKey AND TagStatus = 0)
            THROW 50404, 'Tag does not exist or is not being added.', 1;

        MERGE dbo.ExtendedQueryTagError WITH (HOLDLOCK) as tgt
        USING (SELECT @tagKey TagKey, @errorMessage ErrorMessage, @watermark Watermark) as src
        ON src.TagKey = tgt.TagKey AND src.WaterMark = tgt.Watermark
        WHEN MATCHED THEN UPDATE
        SET CreatedTime = @currentDate,
            ErrorMessage = @errorMessage
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, ErrorMessage, Watermark, CreatedTime)
            VALUES (@tagKey, @errorMessage, @watermark, @currentDate)
        OUTPUT INSERTED.TagKey;

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
    @operationId VARCHAR(32),
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
               TagStatus
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

COMMIT TRANSACTION
GO
