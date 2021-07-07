/***************************************************************************************/
-- STORED PROCEDURE
--     AddExtendedQueryTags
--
-- DESCRIPTION
--    Add a list of extended query tags.
--
-- PARAMETERS
--     @extendedQueryTags
--         * The extended query tag list
--     @maxCount
--         * The max allowed extended query tag count
--     @ready
--         * Indicates whether the new query tags have been fully indexed
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
        IF ((SELECT COUNT(*) FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)) + (SELECT COUNT(*) FROM @extendedQueryTags)) > @maxAllowedCount
             THROW 50409, 'extended query tags exceed max allowed count', 1

        -- Check if tag with same path already exist
        IF EXISTS(SELECT 1 FROM dbo.ExtendedQueryTag WITH(HOLDLOCK) INNER JOIN @extendedQueryTags input ON input.TagPath = dbo.ExtendedQueryTag.TagPath)
            THROW 50409, 'extended query tag(s) already exist', 2

        -- add to extended query tag table with status 0 (Adding)
        INSERT INTO dbo.ExtendedQueryTag
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        OUTPUT INSERTED.TagKey
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, @ready FROM @extendedQueryTags

    COMMIT TRANSACTION
GO
