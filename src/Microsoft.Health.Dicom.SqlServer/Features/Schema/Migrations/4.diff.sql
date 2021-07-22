SET XACT_ABORT ON

BEGIN TRANSACTION

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

    SELECT XQT.*
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

        MERGE INTO dbo.ExtendedQueryTagOperation AS XQTO
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

        SELECT XQT.*
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

        DECLARE @updatedKeys AS dbo.ExtendedQueryTagKeyTableType_1

        -- Update the TagStatus of all rows to Completed (1)
        UPDATE XQT
        SET TagStatus = 1
        OUTPUT INSERTED.TagKey
        INTO @updatedKeys
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @extendedQueryTagKeys AS input ON XQT.TagKey = input.TagKey
        INNER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON input.TagKey = XQTO.TagKey
        WHERE TagStatus = 0

        -- Delete their corresponding operations
        DELETE XQTO
        OUTPUT DELETED.TagKey
        FROM dbo.ExtendedQueryTagOperation AS XQTO
        INNER JOIN @updatedKeys AS uk ON XQTO.TagKey = uk.TagKey

    COMMIT TRANSACTION
GO

COMMIT TRANSACTION
GO
