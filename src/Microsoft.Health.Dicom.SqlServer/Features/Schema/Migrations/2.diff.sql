/*************************************************************
    SQL VERSION 2
*************************************************************/
/*************************************************************
    TABLES
*************************************************************/

/*************************************************************
    Custom Tag Table
    Stores added custom tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    TagLevel can be 0, 1 or 2 to represent Instance, Series or Study level
**************************************************************/
CREATE TABLE dbo.CustomTag (
    TagKey                  BIGINT               NOT NULL, --PK
    TagPath                 VARCHAR(64)          NOT NULL,
    TagVR                   VARCHAR(2)           NOT NULL,
    TagLevel                TINYINT              NOT NULL,
    TagStatus               TINYINT              NOT NULL,
)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTag ON dbo.CustomTag
(
    TagKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_CustomTag_TagPath ON dbo.CustomTag
(
    TagPath
)

/*************************************************************
    SEQUENCES
*************************************************************/
CREATE SEQUENCE dbo.TagKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000
GO

/*************************************************************
    USER DEFINED TYPES
*************************************************************/
/*************************************************************
    The user defined type for AddCustomTagsInput
*************************************************************/
CREATE TYPE dbo.AddCustomTagsInputTableType_1 AS TABLE
(
    TagPath                    VARCHAR(64),  -- Custom Tag Path. Each custom tag take 8 bytes, support upto 8 levels, no delimeter between each level.
    TagVR                      VARCHAR(2),  -- Custom Tag VR.
    TagLevel                   TINYINT  -- Custom Tag level. 0 -- Instance Level, 1 -- Series Level, 2 -- Study Level
)
GO

/*************************************************************
    PROCEDURES
*************************************************************/
/***************************************************************************************/
-- STORED PROCEDURE
--     AddCustomTags
--
-- DESCRIPTION
--    Add a list of custom tags.
--
-- PARAMETERS
--     @customTags
--         * The custom tag list
/***************************************************************************************/
CREATE PROCEDURE dbo.AddCustomTags (
    @customTags dbo.AddCustomTagsInputTableType_1 READONLY)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        
        -- Check if tag with same path already exist
        SELECT TagKey 
        FROM dbo.CustomTag WITH(HOLDLOCK) 
        INNER JOIN @customTags input 
        ON input.TagPath = dbo.CustomTag.TagPath 
	    
        IF @@ROWCOUNT <> 0
            THROW 50409, 'custom tag(s) already exist', 1 

        -- add to custom tag table with status 1(Added)
        INSERT INTO dbo.CustomTag 
            (TagKey, TagPath, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagVR, TagLevel, 1 FROM @customTags
        
    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteCustomTag
--
-- DESCRIPTION
--    Delete specific custom tag
--
-- PARAMETERS
--     @tagPath
--         * The custom tag path
--     @dataType
--         * the data type of custom tag. 0 -- String, 1 -- BigInt, 2 -- Double, 3 -- DateTime, 4 -- PersonName
/***************************************************************************************/
CREATE PROCEDURE dbo.DeleteCustomTag (
    @tagPath VARCHAR(64),
    @dataType TINYINT)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        
        -- Check if tag exsit
        DECLARE @tagStatus TINYINT
        DECLARE @tagKey TINYINT
        SELECT @tagKey = TagKey, @tagStatus = TagStatus
        FROM dbo.CustomTag WITH(HOLDLOCK) 
        WHERE dbo.CustomTag.TagPath = @tagPath

        IF @@ROWCOUNT = 0
            THROW 50404, 'custom tag not found', 1 

        -- check if status is Added
        IF @tagStatus <> 1 
            THROW 50412, 'custom tag is not in status Added', 1

        -- Update status to deindexing
        UPDATE dbo.CustomTag
        SET TagStatus = 2
        WHERE TagKey = @tagKey

        -- Delete index data
        IF @dataType = 0
            DELETE FROM dbo.CustomTagString WHERE TagKey = @tagKey
        ELSE IF @dataType = 1
            DELETE FROM dbo.CustomTagBigInt WHERE TagKey = @tagKey
        ELSE IF @dataType = 2
            DELETE FROM dbo.CustomTagDouble WHERE TagKey = @tagKey
        ELSE IF @dataType = 3
            DELETE FROM dbo.CustomTagDateTime WHERE TagKey = @tagKey
        ELSE
            DELETE FROM dbo.CustomTagPersonName WHERE TagKey = @tagKey

        -- Delete tag
        DELETE FROM dbo.CustomTag 
        WHERE TagKey = @tagKey
        
    COMMIT TRANSACTION
GO
