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

CREATE NONCLUSTERED INDEX IX_CustomTag_TagPath ON dbo.CustomTag
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
    USER DEFINED TABLES
*************************************************************/
/*************************************************************
    The user defined table for AddCustomTagsInput
*************************************************************/
CREATE TYPE dbo.AddCustomTagsInputTableType_1 AS TABLE
(
    TagPath                    VARCHAR(64),  -- Custom Tag Path. Each custom tag take 8 bytes, support upto 8 levels, no delimeter between each level.
    TagVR                      VARCHAR(2),  -- Custom Tag VR.
    TagLevel                   TINYINT,  -- Custom Tag level. 0 -- Instance Level, 1 -- Series Level, 2 -- Study Level
    TagStatus                  TINYINT
)

GO

/*************************************************************
    PROCEDURES
*************************************************************/

CREATE PROCEDURE dbo.AddCustomTags (
    @customTags dbo.AddCustomTagsInputTableType_1 READONLY)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @duplicateCount BIGINT

    BEGIN TRANSACTION
        
        -- Check if tag with same path already exist
        SELECT TagKey FROM dbo.CustomTag INNER JOIN @customTags input ON input.TagPath = dbo.CustomTag.TagPath
	    
        SET @duplicateCount = @@ROWCOUNT
        IF @duplicateCount <> 0
            THROW 50409, 'custom tag(s) already exist', @duplicateCount; 

        -- add to custom tag table 
        INSERT INTO dbo.CustomTag 
            (TagKey, TagPath, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagVR,TagLevel, TagStatus FROM @customTags
        
    COMMIT TRANSACTION
GO
