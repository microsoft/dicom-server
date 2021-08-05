
/*************************************************************
Wrapping up in a transaction except CREATE FULLTEXT INDEX which is non-transactional script. Since there are no slow scripts(all the statements i.e. CREATE TABLE/INDEX/STORED PROC and ALTER STORED PROC are faster) so keeping all in one transaction.
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
**************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    Extended Query Tag Table
    Stores added extended query tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    TagPrivateCreator is identification code of private tag implementer, only apply to private tag.
    TagLevel can be 0, 1 or 2 to represent Instance, Series or Study level
    TagStatus can be 0, 1 or 2 to represent Adding, Ready or Deleting
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTag')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTag (
        TagKey                  INT                  NOT NULL, --PK
        TagPath                 VARCHAR(64)          NOT NULL,
        TagVR                   VARCHAR(2)           NOT NULL,
        TagPrivateCreator       NVARCHAR(64)         NULL, 
        TagLevel                TINYINT              NOT NULL,
        TagStatus               TINYINT              NOT NULL
    )
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTag' AND object_id = OBJECT_ID('dbo.ExtendedQueryTag'))
BEGIN
	CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTag ON dbo.ExtendedQueryTag
    (
        TagKey
    )
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IX_ExtendedQueryTag_TagPath' AND object_id = OBJECT_ID('dbo.ExtendedQueryTag'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTag_TagPath ON dbo.ExtendedQueryTag
    (
        TagPath
    )
END
GO

/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to String
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagString')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagString (
    TagKey                  INT                  NOT NULL, --PK
    TagValue                NVARCHAR(64)         NOT NULL,
    StudyKey                BIGINT               NOT NULL, --FK
    SeriesKey               BIGINT               NULL,     --FK
    InstanceKey             BIGINT               NULL,     --FK
    Watermark               BIGINT               NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTagString' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagString'))
BEGIN
	CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagString ON dbo.ExtendedQueryTagString
    (
        TagKey,
        TagValue,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
END
GO

/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to Long
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagLong')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagLong (
        TagKey                  INT                  NOT NULL, --PK
        TagValue                BIGINT               NOT NULL,
        StudyKey                BIGINT               NOT NULL, --FK
        SeriesKey               BIGINT               NULL,     --FK
        InstanceKey             BIGINT               NULL,     --FK
        Watermark               BIGINT               NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTagLong' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagLong'))
BEGIN
	CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagLong ON dbo.ExtendedQueryTagLong
    (
        TagKey,
        TagValue,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
END
GO

/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to Double
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagDouble')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagDouble (
        TagKey                  INT                  NOT NULL, --PK
        TagValue                FLOAT(53)            NOT NULL,
        StudyKey                BIGINT               NOT NULL, --FK
        SeriesKey               BIGINT               NULL,     --FK
        InstanceKey             BIGINT               NULL,     --FK
        Watermark               BIGINT               NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTagDouble' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagDouble'))
BEGIN
	CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDouble ON dbo.ExtendedQueryTagDouble
    (
        TagKey,
        TagValue,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
END
GO

/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to DateTime
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagDateTime')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagDateTime (
        TagKey                  INT                  NOT NULL, --PK
        TagValue                DATETIME2(7)         NOT NULL,
        StudyKey                BIGINT               NOT NULL, --FK
        SeriesKey               BIGINT               NULL,     --FK
        InstanceKey             BIGINT               NULL,     --FK
        Watermark               BIGINT               NOT NULL
    ) WITH (DATA_COMPRESSION = PAGE)
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTagDateTime' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagDateTime'))
BEGIN
	CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
    (
        TagKey,
        TagValue,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
END
GO

/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to PersonName
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
          For example, with multiple instances in a series, while indexing a series level tag,
          the Watermark is used to ensure that if there are different values between instances,
          the value on the instance with the highest watermark wins.
    Note: The primary key is designed on the assumption that tags only occur once in an instance.
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ExtendedQueryTagPersonName')
BEGIN
    CREATE TABLE dbo.ExtendedQueryTagPersonName (
        TagKey                  INT                  NOT NULL, --FK
        TagValue                NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI NOT NULL,
        StudyKey                BIGINT               NOT NULL, --FK
        SeriesKey               BIGINT               NULL,     --FK
        InstanceKey             BIGINT               NULL,     --FK
        Watermark               BIGINT               NOT NULL,
        WatermarkAndTagKey      AS CONCAT(TagKey, '.', Watermark), --PK
        TagValueWords           AS REPLACE(REPLACE(TagValue, '^', ' '), '=', ' ') PERSISTED,
    ) WITH (DATA_COMPRESSION = PAGE)
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTagPersonName' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName'))
BEGIN
	CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName ON dbo.ExtendedQueryTagPersonName
    (
        TagKey,
        TagValue,
        StudyKey,
        SeriesKey,
        InstanceKey
    )
END

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey' AND object_id = OBJECT_ID('dbo.ExtendedQueryTagPersonName'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey ON dbo.ExtendedQueryTagPersonName
    (
        WatermarkAndTagKey
    )
END

/*************************************************************
    The user defined type for AddExtendedQueryTagsInput
*************************************************************/
IF TYPE_ID(N'AddExtendedQueryTagsInputTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.AddExtendedQueryTagsInputTableType_1 AS TABLE
    (
        TagPath                    VARCHAR(64),  -- Extended Query Tag Path. Each extended query tag take 8 bytes, support upto 8 levels, no delimeter between each level.
        TagVR                      VARCHAR(2),  -- Extended Query Tag VR.
        TagPrivateCreator          NVARCHAR(64),  -- Extended Query Tag Private Creator, only valid for private tag.
        TagLevel                   TINYINT  -- Extended Query Tag level. 0 -- Instance Level, 1 -- Series Level, 2 -- Study Level
    )
END
GO

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type String
*************************************************************/
IF TYPE_ID(N'InsertStringExtendedQueryTagTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.InsertStringExtendedQueryTagTableType_1 AS TABLE
    (
        TagKey                     INT,
        TagValue                   NVARCHAR(64),
        TagLevel                   TINYINT
    )
END
GO

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Double
*************************************************************/
IF TYPE_ID(N'InsertDoubleExtendedQueryTagTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.InsertDoubleExtendedQueryTagTableType_1 AS TABLE
    (
        TagKey                     INT,
        TagValue                   FLOAT(53),
        TagLevel                   TINYINT
    )
END
GO

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Long
*************************************************************/
IF TYPE_ID(N'InsertLongExtendedQueryTagTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.InsertLongExtendedQueryTagTableType_1 AS TABLE
    (
        TagKey                     INT,
        TagValue                   BIGINT,
        TagLevel                   TINYINT
    )
END
GO

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Date Time
*************************************************************/
IF TYPE_ID(N'InsertDateTimeExtendedQueryTagTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.InsertDateTimeExtendedQueryTagTableType_1 AS TABLE
    (
        TagKey                     INT,
        TagValue                   DATETIME2(7),
        TagLevel                   TINYINT
    )
END
GO

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Person Name
*************************************************************/
IF TYPE_ID(N'InsertPersonNameExtendedQueryTagTableType_1') IS NULL
BEGIN
    CREATE TYPE dbo.InsertPersonNameExtendedQueryTagTableType_1 AS TABLE
    (
        TagKey                     INT,
        TagValue                   NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI,
        TagLevel                   TINYINT
    )
END
GO

/*************************************************************
    Sequence for generating sequential unique ids
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.sequences
    WHERE name = 'TagKeySequence')
BEGIN
    CREATE SEQUENCE dbo.TagKeySequence
        AS INT
        START WITH 1
        INCREMENT BY 1
        MINVALUE 1
        NO CYCLE
        CACHE 10000
END
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
    FROM    dbo.ExtendedQueryTagString
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagLong
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagDouble
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagDateTime
    WHERE   StudyKey = @studyKey
    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)
    AND     InstanceKey = ISNULL(@instanceKey, InstanceKey)

    DELETE
    FROM    dbo.ExtendedQueryTagPersonName
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
        FROM    dbo.ExtendedQueryTagString
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   StudyKey = @studyKey
        AND     SeriesKey = ISNULL(@seriesKey, SeriesKey)

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
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
        FROM    dbo.ExtendedQueryTagString
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   StudyKey = @studyKey

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   StudyKey = @studyKey
    END

    COMMIT TRANSACTION
GO

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
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY
)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        
        -- Check if tag with same path already exist
        SELECT TagKey 
        FROM dbo.ExtendedQueryTag WITH(HOLDLOCK) 
        INNER JOIN @extendedQueryTags input 
        ON input.TagPath = dbo.ExtendedQueryTag.TagPath 
        
        IF @@ROWCOUNT <> 0
            THROW 50409, 'extended query tag(s) already exist', 1 

        -- add to extended query tag table with status 1(Ready)
        INSERT INTO dbo.ExtendedQueryTag 
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, 1 FROM @extendedQueryTags
        
    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTag(s)
--
-- DESCRIPTION
--     Gets all extended query tags or given extended query tag by tag path
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag to retrieve.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTag (
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
    FROM    dbo.ExtendedQueryTag
    WHERE   TagPath                 = ISNULL(@tagPath, TagPath)
END
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
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTag (
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
        FROM dbo.ExtendedQueryTag WITH(XLOCK) 
        WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1 

        -- check if status is Ready
        IF @tagStatus <> 1
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
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @longExtendedQueryTags
--         * Long extended query tag data
--     @doubleExtendedQueryTags
--         * Double extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
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
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,    
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
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

    -- Insert Extended Query Tags

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagString AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @stringExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Not merge on extended query tag which is being deleted.
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
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

    -- Long Key tags
    IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagLong AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @longExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
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
    IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagDouble AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @doubleExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
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
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagDateTime AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @dateTimeExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
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
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagPersonName AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @personNameExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
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

COMMIT TRANSACTION
GO

IF NOT EXISTS (
    SELECT * 
	FROM sys.fulltext_indexes 
	where object_id = object_id('dbo.ExtendedQueryTagPersonName'))
BEGIN
    CREATE FULLTEXT INDEX ON ExtendedQueryTagPersonName(TagValueWords LANGUAGE 1033)
    KEY INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey
    WITH STOPLIST = OFF;
END
GO
