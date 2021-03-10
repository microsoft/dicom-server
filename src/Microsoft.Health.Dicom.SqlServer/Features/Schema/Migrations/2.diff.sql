/*************************************************************
    Custom Tag Table
    Stores added custom tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    TagLevel can be 0, 1 or 2 to represent Instance, Series or Study level
    TagStatus can be 0, 1 or 2 to represent Reindexing, Added or Deindexing
**************************************************************/
CREATE TABLE dbo.CustomTag (
    TagKey                  INT                  NOT NULL, --PK
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
    TagKey                  INT                  NOT NULL, --PK
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
    TagKey                  INT                  NOT NULL, --PK
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
    TagKey                  INT                  NOT NULL, --PK
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
    TagKey                  INT                  NOT NULL, --PK
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
    TagKey                  INT                  NOT NULL, --FK
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
--     GetCustomTag(s)
--
-- DESCRIPTION
--     Gets all custom tags or given custom tag by tag path
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the custom tag to retrieve.
/***************************************************************************************/
CREATE PROCEDURE dbo.GetCustomTag (
    @tagPath  VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TagKey,
            TagPath,
            TagVR,
            TagLevel,
            TagStatus
    FROM    dbo.CustomTag
    WHERE   TagPath                 = ISNULL(@tagPath, TagPath)
END
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
        DECLARE @tagKey INT
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
/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstance
--
-- DESCRIPTION
--     Removes the specified instance(s) and places them in the DeletedInstance table for later removal
--
-- PARAMETERS
--     @cleanupAfter
--         * The date time offset that the instance can be cleaned up.
--     @createdStatus
--         * Status value representing the created state.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
ALTER PROCEDURE dbo.DeleteInstance (
    @cleanupAfter       DATETIMEOFFSET(0),
    @createdStatus      TINYINT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

    DECLARE @deletedInstances AS TABLE
        (StudyInstanceUid VARCHAR(64),
         SeriesInstanceUid VARCHAR(64),
         SopInstanceUid VARCHAR(64),
         Status TINYINT,
         Watermark BIGINT)

    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT
    DECLARE @deletedDate DATETIME2 = SYSUTCDATETIME()

    -- Get the study, series and instance PK
    SELECT  @studyKey = StudyKey, @seriesKey = SeriesKey, @instanceKey = InstanceKey
    FROM    dbo.Instance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    -- Delete the instance and insert the details into DeletedInstance and ChangeFeed
    DELETE  dbo.Instance
        OUTPUT deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark
        INTO @deletedInstances
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    IF (@@ROWCOUNT = 0)
    BEGIN
        THROW 50404, 'Instance not found', 1;
    END

    -- Deleting indexed instance tags
    DELETE
    FROM    dbo.CustomTagString
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = @seriesKey
    AND     InstanceKey = @instanceKey

    DELETE
    FROM    dbo.CustomTagBigInt
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = @seriesKey
    AND     InstanceKey = @instanceKey

    DELETE
    FROM    dbo.CustomTagDouble
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = @seriesKey
    AND     InstanceKey = @instanceKey

    DELETE
    FROM    dbo.CustomTagDateTime
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = @seriesKey
    AND     InstanceKey = @instanceKey

    DELETE
    FROM    dbo.CustomTagPersonName
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = @seriesKey
    AND     InstanceKey = @instanceKey

    INSERT INTO dbo.DeletedInstance
    (StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter)
    SELECT StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, @deletedDate, 0 , @cleanupAfter
    FROM @deletedInstances

    INSERT INTO dbo.ChangeFeed
    (TimeStamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT @deletedDate, 1, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM @deletedInstances
    WHERE Status = @createdStatus

    UPDATE cf
    SET cf.CurrentWatermark = NULL
    FROM dbo.ChangeFeed cf
    JOIN @deletedInstances d
    ON cf.StudyInstanceUid = d.StudyInstanceUid
        AND cf.SeriesInstanceUid = d.SeriesInstanceUid
        AND cf.SopInstanceUid = d.SopInstanceUid

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance WITH(HOLDLOCK, UPDLOCK)
                    WHERE   StudyKey = @studyKey
                    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid))
    BEGIN
        DELETE
        FROM    dbo.Series
        WHERE   Studykey = @studyKey
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)

        -- Deleting indexed series tags
        DELETE
        FROM    dbo.CustomTagString
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = @seriesKey

        DELETE
        FROM    dbo.CustomTagBigInt
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = @seriesKey

        DELETE
        FROM    dbo.CustomTagDouble
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = @seriesKey

        DELETE
        FROM    dbo.CustomTagDateTime
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = @seriesKey

        DELETE
        FROM    dbo.CustomTagPersonName
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = @seriesKey
    END

    -- If we've removing the series, see if it's the last for a study and if so, remove the study
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Series WITH(HOLDLOCK, UPDLOCK)
                    WHERE   Studykey = @studyKey)
    BEGIN
        DELETE
        FROM    dbo.Study
        WHERE   Studykey = @studyKey

        -- Deleting indexed study tags
        DELETE
        FROM    dbo.CustomTagString
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.CustomTagBigInt
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.CustomTagDouble
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.CustomTagDateTime
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.CustomTagPersonName
        WHERE   StudyKey = @studyKey
    END

    COMMIT TRANSACTION
GO
