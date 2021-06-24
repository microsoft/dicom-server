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
/***************************************************************************************/
ALTER PROCEDURE dbo.AddExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @maxAllowedCount INT
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
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, 0 FROM @extendedQueryTags

        SELECT TagKey
        FROM @extendedQueryTags input
        INNER JOIN dbo.ExtendedQueryTag WITH(HOLDLOCK)
        ON input.TagPath = dbo.ExtendedQueryTag.TagPath

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTag
--
-- DESCRIPTION
--    Delete specific extended query tag
--
-- PARAMETERS
--     @tagPath
--         * The extended query tag path
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
--     @force
--         * A bit if 1 indicates the tag should be deleted regardless of status
/***************************************************************************************/
ALTER PROCEDURE dbo.DeleteExtendedQueryTag (
    @tagPath VARCHAR(64),
    @dataType TINYINT,
    @force BIT = 0
)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        
        DECLARE @tagStatus TINYINT
        DECLARE @tagKey INT
 
        SELECT @tagKey = TagKey, @tagStatus = TagStatus
        FROM dbo.ExtendedQueryTag WITH(XLOCK) 
        WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1 

        -- check if status is Ready
        IF @force = 0 AND @tagStatus <> 1
            THROW 50412, 'extended query tag is not in Ready status', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2 
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION

    BEGIN TRANSACTION

        -- Delete index data
        IF @dataType = 0
            DELETE FROM dbo.ExtendedQueryTagString WHERE TagKey = @tagKey
        ELSE IF @dataType = 1
            DELETE FROM dbo.ExtendedQueryTagLong WHERE TagKey = @tagKey
        ELSE IF @dataType = 2
            DELETE FROM dbo.ExtendedQueryTagDouble WHERE TagKey = @tagKey
        ELSE IF @dataType = 3
            DELETE FROM dbo.ExtendedQueryTagDateTime WHERE TagKey = @tagKey
        ELSE
            DELETE FROM dbo.ExtendedQueryTagPersonName WHERE TagKey = @tagKey

        -- Delete tag
        DELETE FROM dbo.ExtendedQueryTag 
        WHERE TagKey = @tagKey

    COMMIT TRANSACTION
GO
