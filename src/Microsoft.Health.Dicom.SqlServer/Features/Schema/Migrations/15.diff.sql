/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/

SET XACT_ABORT ON

BEGIN TRANSACTION

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
--     @errorCode
--         * The error code
--     @watermark
--         * The watermark
--
-- RETURN VALUE
--     The tag key of the error added.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTagError
    @tagKey INT,
    @errorCode SMALLINT,
    @watermark BIGINT
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON
    BEGIN TRANSACTION

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

        --Check if instance with given watermark and Created status.
        IF NOT EXISTS (SELECT * FROM dbo.Instance WITH (UPDLOCK) WHERE Watermark = @watermark AND Status = 1)
            THROW 50404, 'Instance does not exist or has not been created.', 1;

        --Check if tag exists and in Adding status.
        IF NOT EXISTS (SELECT * FROM dbo.ExtendedQueryTag WITH (UPDLOCK) WHERE TagKey = @tagKey AND TagStatus = 0)
            THROW 50404, 'Tag does not exist or is not being added.', 1;

        -- Add error
        DECLARE @addedCount SMALLINT
        SET @addedCount  = 1
        MERGE dbo.ExtendedQueryTagError WITH (HOLDLOCK) as XQTE
        USING (SELECT @tagKey TagKey, @errorCode ErrorCode, @watermark Watermark) as src
        ON src.TagKey = XQTE.TagKey AND src.WaterMark = XQTE.Watermark
        WHEN MATCHED THEN UPDATE
        SET CreatedTime = @currentDate,
            ErrorCode = @errorCode,
            @addedCount = 0
        WHEN NOT MATCHED THEN
            INSERT (TagKey, ErrorCode, Watermark, CreatedTime)
            VALUES (@tagKey, @errorCode, @watermark, @currentDate)
        OUTPUT INSERTED.TagKey;

        -- Disable query on the tag and update error count
        UPDATE dbo.ExtendedQueryTag
        SET QueryStatus = 0, ErrorCount = ErrorCount + @addedCount
        WHERE TagKey = @tagKey

    COMMIT TRANSACTION
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

        -- Check if total count exceed @maxCount
        -- HOLDLOCK to prevent adding queryTags from other transactions at same time, UPDLOCK to prevent simultaneous AddExtendedQueryTags calls which could result in deadlock.
        IF (SELECT COUNT(*)
            FROM dbo.ExtendedQueryTag AS XQT WITH(UPDLOCK,HOLDLOCK)
            FULL OUTER JOIN @extendedQueryTags AS input 
            ON XQT.TagPath = input.TagPath) > @maxAllowedCount
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

COMMIT TRANSACTION
