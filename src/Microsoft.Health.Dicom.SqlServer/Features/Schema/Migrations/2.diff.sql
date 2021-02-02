

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
        FROM dbo.CustomTag WITH(UPDLOCK) 
        INNER JOIN @customTags input 
        ON input.TagPath = dbo.CustomTag.TagPath 
	    
        IF @@ROWCOUNT <> 0
            THROW 50409, 'custom tag(s) already exist', 1 

        -- add to custom tag table with status 1(Added)
        INSERT INTO dbo.CustomTag 
            (TagKey, TagPath, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagVR,TagLevel, 1 FROM @customTags
        
    COMMIT TRANSACTION
GO
