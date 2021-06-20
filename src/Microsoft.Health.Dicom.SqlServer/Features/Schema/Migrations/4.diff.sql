/***************************************************************************************/
-- STORED PROCEDURE
--     UpsertExtendedQueryTags
--
-- DESCRIPTION
--    Add a list of extended query tags.
--
-- PARAMETERS
--     @extendedQueryTags
--         * The extended query tag list
--     @maxCount
--         * The max allowed extended query tag count
/***************************************************************************************/
ALTER PROCEDURE dbo.UpsertExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @maxAllowedCount INT
)
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        -- Query all of the unique tag paths and place them in a temp table called '#NewTags'
        SELECT input.*
        INTO #NewTags
        FROM
        (
            SELECT TagPath
            FROM @extendedQueryTags input
            EXCEPT
            SELECT TagPath
            FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)
        ) as newTagPaths
        INNER JOIN @extendedQueryTags input
        ON newTagPaths.TagPath = input.TagPath

        -- Ensure we won't add too many
        IF ((SELECT COUNT(*) FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)) + (SELECT COUNT(*) FROM #NewTags)) > @maxAllowedCount
             THROW 50409, 'extended query tags exceed max allowed count', 1

        -- Add the extended query tags into the table with a status of 255 to indicate it's not ready for indexing
        INSERT INTO dbo.ExtendedQueryTag 
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, 255 FROM #NewTags

        -- Return the keys for the tags in the input
        SELECT TagKey
        FROM @extendedQueryTags input
        INNER JOIN dbo.ExtendedQueryTag WITH(HOLDLOCK)
        ON input.TagPath = dbo.ExtendedQueryTag.TagPath

    COMMIT TRANSACTION
GO
