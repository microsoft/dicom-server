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
        -- HOLDLOCK to prevent adding queryTags from other transactions at same time.
        IF (SELECT COUNT(*)
            FROM dbo.ExtendedQueryTag AS XQT WITH(HOLDLOCK)
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