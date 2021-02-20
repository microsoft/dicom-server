/*************************************************************
    Custom Tag Table
    Stores added custom tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    TagLevel can be 0, 1 or 2 to represent Instance, Series or Study level
    TagStatus can be 0, 1 or 2 to represent Reindexing, Added or Deindexing
**************************************************************/
CREATE TABLE dbo.CustomTag (
    TagKey                  BIGINT               NOT NULL, --PK
    TagPath                 VARCHAR(64)          NOT NULL,
    TagVR                   VARCHAR(2)           NOT NULL,
    TagLevel                TINYINT              NOT NULL,
    TagStatus               TINYINT              NOT NULL
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
    Custom Tag Data Table for VR Types mapping to String
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.CustomTagString (
    TagKey                  BIGINT               NOT NULL, --PK
    TagValue                NVARCHAR(64)         NOT NULL,
    StudyKey                BIGINT               NOT NULL, --FK
    SeriesKey               BIGINT               NULL,     --FK
    InstanceKey             BIGINT               NULL,     --FK
    Watermark               BIGINT               NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTagString ON dbo.CustomTagString
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

/*************************************************************
    Custom Tag Data Table for VR Types mapping to BigInt
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.CustomTagBigInt (
    TagKey                  BIGINT               NOT NULL, --PK
    TagValue                BIGINT               NOT NULL,
    StudyKey                BIGINT               NOT NULL, --FK
    SeriesKey               BIGINT               NULL,     --FK
    InstanceKey             BIGINT               NULL,     --FK
    Watermark               BIGINT               NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTagBigInt ON dbo.CustomTagBigInt
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

/*************************************************************
    Custom Tag Data Table for VR Types mapping to Double
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.CustomTagDouble (
    TagKey                  BIGINT               NOT NULL, --PK
    TagValue                FLOAT(53)            NOT NULL,
    StudyKey                BIGINT               NOT NULL, --FK
    SeriesKey               BIGINT               NULL,     --FK
    InstanceKey             BIGINT               NULL,     --FK
    Watermark               BIGINT               NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTagDouble ON dbo.CustomTagDouble
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

/*************************************************************
    Custom Tag Data Table for VR Types mapping to DateTime
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.CustomTagDateTime (
    TagKey                  BIGINT               NOT NULL, --PK
    TagValue                DATETIME2(7)         NOT NULL,
    StudyKey                BIGINT               NOT NULL, --FK
    SeriesKey               BIGINT               NULL,     --FK
    InstanceKey             BIGINT               NULL,     --FK
    Watermark               BIGINT               NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTagDateTime ON dbo.CustomTagDateTime
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

/*************************************************************
    Custom Tag Data Table for VR Types mapping to PersonName
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
	Note: The primary key is designed on the assumption that tags only occur once in an instance.
**************************************************************/
CREATE TABLE dbo.CustomTagPersonName (
    TagKey                  BIGINT               NOT NULL, --FK
    TagValue                NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI NOT NULL,
    StudyKey                BIGINT               NOT NULL, --FK
    SeriesKey               BIGINT               NULL,     --FK
    InstanceKey             BIGINT               NULL,     --FK
    Watermark               BIGINT               NOT NULL,
    WatermarkAndTagKey      AS CONCAT(TagKey, '.', Watermark), --PK
    TagValueWords           AS REPLACE(REPLACE(TagValue, '^', ' '), '=', ' ') PERSISTED,
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_CustomTagPersonName ON dbo.CustomTagPersonName
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

CREATE UNIQUE NONCLUSTERED INDEX IXC_CustomTagPersonName_WatermarkAndTagKey ON dbo.CustomTagPersonName
(
    WatermarkAndTagKey
)

CREATE FULLTEXT INDEX ON CustomTagPersonName(TagValueWords LANGUAGE 1033)
KEY INDEX IXC_CustomTagPersonName_WatermarkAndTagKey
WITH STOPLIST = OFF;

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
    Sequence for generating sequential unique ids
**************************************************************/
CREATE SEQUENCE dbo.TagKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000

GO

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
