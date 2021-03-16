/*************************************************************
    Custom Tag Table
    Stores added custom tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    TagPrivateCreator is private creator, only apply to private tag.
    TagLevel can be 0, 1 or 2 to represent Instance, Series or Study level
    TagStatus can be 0, 1 or 2 to represent Reindexing, Added or Deindexing
**************************************************************/
CREATE TABLE dbo.CustomTag (
    TagKey                  INT                  NOT NULL, --PK
    TagPath                 VARCHAR(64)          NOT NULL,
    TagVR                   VARCHAR(2)           NOT NULL,
    TagPrivateCreator       NVARCHAR(64)         NULL, 
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
    Table valued parameter to insert into Custom table for data type String
*************************************************************/
CREATE TYPE dbo.InsertStringCustomTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   NVARCHAR(64),
    TagLevel                   TINYINT
)
GO

/*************************************************************
    Table valued parameter to insert into Custom table for data type Double
*************************************************************/
CREATE TYPE dbo.InsertDoubleCustomTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   FLOAT(53),
    TagLevel                   TINYINT
)
GO

/*************************************************************
    Table valued parameter to insert into Custom table for data type Big Int
*************************************************************/
CREATE TYPE dbo.InsertBigIntCustomTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   BIGINT,
    TagLevel                   TINYINT
)
GO

/*************************************************************
    Table valued parameter to insert into Custom table for data type Date Time
*************************************************************/
CREATE TYPE dbo.InsertDateTimeCustomTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   DATETIME2(7),
    TagLevel                   TINYINT
)
GO

/*************************************************************
    Table valued parameter to insert into Custom table for data type Person Name
*************************************************************/
CREATE TYPE dbo.InsertPersonNameCustomTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI,
    TagLevel                   TINYINT
)
GO

/*************************************************************
    Sequence for generating sequential unique ids
**************************************************************/
CREATE SEQUENCE dbo.TagKeySequence
    AS INT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000

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
    SELECT  @studyKey = StudyKey,
    @seriesKey = CASE @seriesInstanceUid WHEN NULL THEN NULL ELSE SeriesKey END,
    @instanceKey = CASE @sopInstanceUid WHEN NULL THEN NULL ELSE InstanceKey END
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
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.CustomTagBigInt
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.CustomTagDouble
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.CustomTagDateTime
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.CustomTagPersonName
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

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
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.CustomTagBigInt
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.CustomTagDouble
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.CustomTagDateTime
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.CustomTagPersonName
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
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
    @customTags dbo.AddCustomTagsInputTableType_1 READONLY
)
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
            TagPrivateCreator,
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
    @dataType TINYINT
)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        
        DECLARE @tagStatus TINYINT
        DECLARE @tagKey INT        
  
        SELECT @tagKey = TagKey, @tagStatus = TagStatus
        FROM dbo.CustomTag WITH(XLOCK) 
        WHERE dbo.CustomTag.TagPath = @tagPath

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'custom tag not found', 1 

        -- check if status is Added
        IF @tagStatus <> 1
            THROW 50412, 'custom tag is not in status Added', 1

        -- Update status to Deindexing
        UPDATE dbo.CustomTag
        SET TagStatus = 2 
        WHERE dbo.CustomTag.TagKey = @tagKey

    COMMIT TRANSACTION

    BEGIN TRANSACTION

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

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstance
--
-- DESCRIPTION
--     Adds a DICOM instance.
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @patientId
--         * The Id of the patient.
--     @patientName
--         * The name of the patient.
--     @referringPhysicianName
--         * The referring physician name.
--     @studyDate
--         * The study date.
--     @studyDescription
--         * The study description.
--     @accessionNumber
--         * The accession number associated for the study.
--     @modality
--         * The modality associated for the series.
--     @performedProcedureStepStartDate
--         * The date when the procedure for the series was performed.
--     @stringCustomTags
--         * String custom tag data
--     @bigIntCustomTags
--         * BigInt custom tag data
--     @doubleCustomTags
--         * Double custom tag data
--     @dateTimeCustomTags
--         * DateTime custom tag data
--     @personNameCustomTags
--         * PersonName custom tag data
-- RETURN VALUE
--     The watermark (version).
------------------------------------------------------------------------
ALTER PROCEDURE dbo.AddInstance
    @studyInstanceUid                   VARCHAR(64),
    @seriesInstanceUid                  VARCHAR(64),
    @sopInstanceUid                     VARCHAR(64),
    @patientId                          NVARCHAR(64),
    @patientName                        NVARCHAR(325) = NULL,
    @referringPhysicianName             NVARCHAR(325) = NULL,
    @studyDate                          DATE = NULL,
    @studyDescription                   NVARCHAR(64) = NULL,
    @accessionNumber                    NVARCHAR(64) = NULL,
    @modality                           NVARCHAR(16) = NULL,
    @performedProcedureStepStartDate    DATE = NULL,                
    @stringCustomTags dbo.InsertStringCustomTagTableType_1 READONLY,    
    @bigIntCustomTags dbo.InsertBigIntCustomTagTableType_1 READONLY,
    @doubleCustomTags dbo.InsertDoubleCustomTagTableType_1 READONLY,
    @dateTimeCustomTags dbo.InsertDateTimeCustomTagTableType_1 READONLY,
    @personNameCustomTags dbo.InsertPersonNameCustomTagTableType_1 READONLY,
    @initialStatus                      TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @existingStatus TINYINT
    DECLARE @newWatermark BIGINT
    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT

    SELECT @existingStatus = Status
    FROM dbo.Instance
    WHERE StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid

    IF @@ROWCOUNT <> 0    
        -- The instance already exists. Set the state = @existingStatus to indicate what state it is in.
        THROW 50409, 'Instance already exists', @existingStatus;    

    -- The instance does not exist, insert it.
    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence
    SET @instanceKey = NEXT VALUE FOR dbo.InstanceKeySequence

    -- Insert Study
    SELECT @studyKey = StudyKey
    FROM dbo.Study WITH(UPDLOCK)
    WHERE StudyInstanceUid = @studyInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

        INSERT INTO dbo.Study
            (StudyKey, StudyInstanceUid, PatientId, PatientName, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@studyKey, @studyInstanceUid, @patientId, @patientName, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Study
        SET PatientId = @patientId, PatientName = @patientName, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
        WHERE StudyKey = @studyKey
    END

    -- Insert Series
    SELECT @seriesKey = SeriesKey
    FROM dbo.Series WITH(UPDLOCK)
    WHERE StudyKey = @studyKey
    AND SeriesInstanceUid = @seriesInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @seriesKey = NEXT VALUE FOR dbo.SeriesKeySequence

        INSERT INTO dbo.Series
            (StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate)
        VALUES
            (@studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Series
        SET Modality = @modality, PerformedProcedureStepStartDate = @performedProcedureStepStartDate
        WHERE SeriesKey = @seriesKey
        AND StudyKey = @studyKey
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate)

    -- Insert Custom Tags

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringCustomTags)
    BEGIN      
        MERGE INTO dbo.CustomTagString AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @stringCustomTags input
            INNER JOIN dbo.CustomTag WITH (REPEATABLEREAD) 
            ON dbo.CustomTag.TagKey = input.TagKey
            -- Not merge on custom tag which is being deleted.
            AND dbo.CustomTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey      
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            -- When TagLevel is not Study, we should fill SeriesKey
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            -- When TagLevel is Instance, we should fill InstanceKey
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);        
    END

    -- BigInt Key tags
    IF EXISTS (SELECT 1 FROM @bigIntCustomTags)
    BEGIN      
        MERGE INTO dbo.CustomTagBigInt AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @bigIntCustomTags input
            INNER JOIN dbo.CustomTag WITH (REPEATABLEREAD) 
            ON dbo.CustomTag.TagKey = input.TagKey            
            AND dbo.CustomTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);        
    END

    -- Double Key tags
    IF EXISTS (SELECT 1 FROM @doubleCustomTags)
    BEGIN      
        MERGE INTO dbo.CustomTagDouble AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @doubleCustomTags input
            INNER JOIN dbo.CustomTag WITH (REPEATABLEREAD) 
            ON dbo.CustomTag.TagKey = input.TagKey            
            AND dbo.CustomTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);        
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeCustomTags)
    BEGIN      
        MERGE INTO dbo.CustomTagDateTime AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @dateTimeCustomTags input
            INNER JOIN dbo.CustomTag WITH (REPEATABLEREAD) 
            ON dbo.CustomTag.TagKey = input.TagKey            
            AND dbo.CustomTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);        
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameCustomTags)
    BEGIN      
        MERGE INTO dbo.CustomTagPersonName AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @personNameCustomTags input
            INNER JOIN dbo.CustomTag WITH (REPEATABLEREAD) 
            ON dbo.CustomTag.TagKey = input.TagKey            
            AND dbo.CustomTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            UPDATE SET T.Watermark = @newWatermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @newWatermark);        
    END

    SELECT @newWatermark

    COMMIT TRANSACTION
GO
