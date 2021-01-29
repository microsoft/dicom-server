---- VERSION 2 -----
/*************************************************************
    Custom Tag Table
    Store custom tag information
    
    Current Instance State
    CurrentWatermark = null,               Current State = Deleted
    CurrentWatermark = OriginalWatermark,  Current State = Created
    CurrentWatermark <> OriginalWatermark, Current State = Replaced
**************************************************************/
CREATE TABLE dbo.CustomTag (
    [Key]                   BIGINT              NOT NULL,  -- Primary Key
    Path                    VARCHAR(64)         NOT NULL,  -- Custom Tag Path. Each custom tag take 8 bytes, support upto 8 levels, no delimeter between each level.
    VR                      VARCHAR(2)             NOT NULL,  -- Custom Tag VR.
    Level                   TINYINT             NOT NULL,  -- Custom Tag level. 0 -- Instance Level, 1 -- Series Level, 2 -- Study Level
    Status                  TINYINT             NOT NULL,  -- Custom Tag Status. 0 -- Reindexing, 1 -- Added, 2 -- Deindexing
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTag ON dbo.CustomTag
(
    [Key]
)


/*Sequences */
CREATE SEQUENCE dbo.CustomTagKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000
GO

/* UDT*/
CREATE TYPE dbo.AddCustomTagsInputTableType_1 AS TABLE
(
    Path                    VARCHAR(64),  -- Custom Tag Path. Each custom tag take 8 bytes, support upto 8 levels, no delimeter between each level.
    VR                      VARCHAR(2),  -- Custom Tag VR.
    Level                   TINYINT,  -- Custom Tag level. 0 -- Instance Level, 1 -- Series Level, 2 -- Study Level
    Status                  TINYINT
)

GO
/*Procedure*/

CREATE PROCEDURE dbo.AddCustomTags (
    @customTags dbo.AddCustomTagsInputTableType_1 READONLY)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @key BIGINT
    BEGIN TRANSACTION
        
        -- Check if tag with same path already exist
        SELECT [Key] FROM dbo.CustomTag INNER JOIN @customTags input ON input.Path = dbo.CustomTag.Path
	    
        IF @@ROWCOUNT <> 0
            THROW 50409, 'custom tag(s) already exist', 0; --TODO:figure out better state (0)

        -- add to table 
        INSERT INTO dbo.CustomTag 
            ([Key], Path, VR, Level, Status)
        SELECT NEXT VALUE FOR CustomTagKeySequence, Path, VR, Level, 0 FROM @customTags -- status 0 means reindexing
        
        -- return tags
        SELECT [Key],customtag.Path,customtag.VR, customtag.Level,customtag.Status FROM dbo.CustomTag customtag INNER JOIN @customTags input ON input.Path = customtag.Path

    COMMIT TRANSACTION
GO
