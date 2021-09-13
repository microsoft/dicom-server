/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
******************************************************************************************/
SET XACT_ABORT ON

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionId'
            AND object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    ALTER TABLE dbo.Instance
    ADD PartitionId VARCHAR(32)
    DEFAULT ('00000000000000000000000000000000')
END
GO

UPDATE dbo.Instance
    SET PartitionId = '00000000000000000000000000000000'
    WHERE PartitionId = NULL
GO

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionId'
            AND object_id = OBJECT_ID('dbo.Study')
)
BEGIN
    ALTER TABLE dbo.Study
    ADD PartitionId VARCHAR(32)
    DEFAULT ('00000000000000000000000000000000')
END
GO

UPDATE dbo.Study
    SET PartitionId = '00000000000000000000000000000000'
    WHERE PartitionId = NULL
GO

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionId'
            AND object_id = OBJECT_ID('dbo.Series')
)
BEGIN
    ALTER TABLE dbo.Series
    ADD PartitionId VARCHAR(32)
    DEFAULT ('00000000000000000000000000000000')
END
GO

UPDATE dbo.Series
    SET PartitionId = '00000000000000000000000000000000'
    WHERE PartitionId = NULL
GO

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionId'
            AND object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    ALTER TABLE dbo.DeletedInstance
    ADD PartitionId VARCHAR(32)
    DEFAULT ('00000000000000000000000000000000')
END
GO

UPDATE dbo.DeletedInstance
    SET PartitionId = '00000000000000000000000000000000'
    WHERE PartitionId = NULL
GO

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PartitionId'
            AND object_id = OBJECT_ID('dbo.ChangeFeed')
)
BEGIN
    ALTER TABLE dbo.ChangeFeed
    ADD PartitionId VARCHAR(32)
    DEFAULT ('00000000000000000000000000000000')
END
GO

UPDATE dbo.ChangeFeed
    SET PartitionId = '00000000000000000000000000000000'
    WHERE PartitionId = NULL
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
--     @partitionId
--         * The Partition ID.
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
    @partitionId                        VARCHAR(32),
    @patientId                          NVARCHAR(64),
    @patientName                        NVARCHAR(325) = NULL,
    @referringPhysicianName             NVARCHAR(325) = NULL,
    @studyDate                          DATE = NULL,
    @studyDescription                   NVARCHAR(64) = NULL,
    @accessionNumber                    NVARCHAR(64) = NULL,
    @modality                           NVARCHAR(16) = NULL,
    @performedProcedureStepStartDate    DATE = NULL,
    @patientBirthDate                   DATE = NULL,
    @manufacturerModelName              NVARCHAR(64) = NULL,
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
        AND PartitionId = @partitionId

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
        AND PartitionId = @partitionId

    IF @@ROWCOUNT = 0
    BEGIN
        SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

        INSERT INTO dbo.Study
            (StudyKey, StudyInstanceUid, PartitionId, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@studyKey, @studyInstanceUid, @partitionId, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Study
        SET PatientId = @patientId, PatientName = @patientName, PatientBirthDate = @patientBirthDate, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
        WHERE StudyKey = @studyKey
    END

    -- Insert Series
    SELECT @seriesKey = SeriesKey
    FROM dbo.Series WITH(UPDLOCK)
    WHERE StudyKey = @studyKey
    AND SeriesInstanceUid = @seriesInstanceUid
    AND PartitionId = @partitionId

    IF @@ROWCOUNT = 0
    BEGIN
        SET @seriesKey = NEXT VALUE FOR dbo.SeriesKeySequence

        INSERT INTO dbo.Series
            (StudyKey, SeriesKey, SeriesInstanceUid, PartitionId, Modality, PerformedProcedureStepStartDate, ManufacturerModelName)
        VALUES
            (@studyKey, @seriesKey, @seriesInstanceUid, @partitionId, @modality, @performedProcedureStepStartDate, @manufacturerModelName)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Series
        SET Modality = @modality, PerformedProcedureStepStartDate = @performedProcedureStepStartDate, ManufacturerModelName = @manufacturerModelName
        WHERE SeriesKey = @seriesKey
        AND StudyKey = @studyKey
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, PartitionId, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @partitionId, @newWatermark, @initialStatus, @currentDate, @currentDate)

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

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatus
--
-- DESCRIPTION
--     Updates a DICOM instance status.
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @partitionId
--         * The Partition ID.
--     @watermark
--         * The watermark.
--     @status
--         * The new status to update to.
--
-- RETURN VALUE
--     None
--
ALTER PROCEDURE dbo.UpdateInstanceStatus
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @partitionId        VARCHAR(32),
    @watermark          BIGINT,
    @status             TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE StudyInstanceUid = @studyInstanceUid
    AND SeriesInstanceUid = @seriesInstanceUid
    AND SopInstanceUid = @sopInstanceUid
    AND Watermark = @watermark
    AND PartitionId = @partitionId

    IF @@ROWCOUNT = 0
    BEGIN
        -- The instance does not exist. Perhaps it was deleted?
        THROW 50404, 'Instance does not exist', 1;
    END

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Timestamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark, PartitionId)
    VALUES
        (@currentDate, 0, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark, @partitionId)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid
        AND PartitionId       = @partitionId

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstance
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @partitionId
--         * The Partition ID.
/***************************************************************************************/
ALTER PROCEDURE dbo.GetInstance (
    @validStatus        TINYINT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL,
    @partitionId        VARCHAR(32)
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON


    SELECT  StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            Watermark
    FROM    dbo.Instance
    WHERE   StudyInstanceUid        = @studyInstanceUid
            AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
            AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
            AND PartitionId         = @partitionId
            AND Status              = @validStatus

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
    @sopInstanceUid     VARCHAR(64) = null,
    @partitionId        VARCHAR(32)
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

    DECLARE @deletedInstances AS TABLE
        (StudyInstanceUid VARCHAR(64),
            SeriesInstanceUid VARCHAR(64),
            SopInstanceUid VARCHAR(64),
            PartitionId VARCHAR(32),
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
    AND     PartitionId = @partitionId

    -- Delete the instance and insert the details into DeletedInstance and ChangeFeed
    DELETE  dbo.Instance
        OUTPUT deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark, deleted.PartitionId
        INTO @deletedInstances
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)
    AND     PartitionId = @partitionId

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
    (StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, PartitionId, Watermark, DeletedDateTime, RetryCount, CleanupAfter)
    SELECT StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, PartitionId, Watermark, @deletedDate, 0 , @cleanupAfter
    FROM @deletedInstances

    INSERT INTO dbo.ChangeFeed
    (TimeStamp, Action, StudyInstanceUid, SeriesInstanceUid, PartitionId, SopInstanceUid, OriginalWatermark)
    SELECT @deletedDate, 1, StudyInstanceUid, SeriesInstanceUid, PartitionId, SopInstanceUid, Watermark
    FROM @deletedInstances
    WHERE Status = @createdStatus

    UPDATE cf
    SET cf.CurrentWatermark = NULL
    FROM dbo.ChangeFeed cf
    JOIN @deletedInstances d
    ON cf.StudyInstanceUid = d.StudyInstanceUid
        AND cf.SeriesInstanceUid = d.SeriesInstanceUid
        AND cf.SopInstanceUid = d.SopInstanceUid
        AND cf.PartitionId = d.PartitionId

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance WITH(HOLDLOCK, UPDLOCK)
                    WHERE   StudyKey = @studyKey
                    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
                    AND     PartitionId = @partitionId)
    BEGIN
        DELETE
        FROM    dbo.Series
        WHERE   Studykey = @studyKey
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     PartitionId = @partitionId

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
--     RetrieveDeletedInstance
--
-- DESCRIPTION
--     Retrieves deleted instances where the cleanupAfter is less than the current date in and the retry count hasn't been exceeded
--
-- PARAMETERS
--     @count
--         * The number of entries to return
--     @maxRetries
--         * The maximum number of times to retry a cleanup
/***************************************************************************************/
ALTER PROCEDURE dbo.RetrieveDeletedInstance
    @count          INT,
    @maxRetries     INT
AS
    SET NOCOUNT ON

    SELECT  TOP (@count) StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, PartitionId
    FROM    dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteDeletedInstance
--
-- DESCRIPTION
--     Removes a deleted instance from the DeletedInstance table
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @partitionId
--         * The Partition ID.
--     @watermark
--         * The watermark of the entry
/***************************************************************************************/
ALTER PROCEDURE dbo.DeleteDeletedInstance(
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @partitionId        VARCHAR(32),
    @watermark          BIGINT
)
AS
    SET NOCOUNT ON

    DELETE
    FROM    dbo.DeletedInstance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = @seriesInstanceUid
    AND     SopInstanceUid = @sopInstanceUid
    AND     Watermark = @watermark
    AND     PartitionId = @partitionId
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     IncrementDeletedInstanceRetry
--
-- DESCRIPTION
--     Increments the retryCount of and retryAfter of a deleted instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @partitionId
--         * The Partition ID.
--     @watermark
--         * The watermark of the entry
--     @cleanupAfter
--         * The next date time to attempt cleanup
--
-- RETURN VALUE
--     The retry count.
--
/***************************************************************************************/
ALTER PROCEDURE dbo.IncrementDeletedInstanceRetry(
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @partitionId        VARCHAR(32),
    @watermark          BIGINT,
    @cleanupAfter       DATETIMEOFFSET(0)
)
AS
    SET NOCOUNT ON

    DECLARE @retryCount INT

    UPDATE  dbo.DeletedInstance
    SET     @retryCount = RetryCount = RetryCount + 1,
            CleanupAfter = @cleanupAfter
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = @seriesInstanceUid
    AND     SopInstanceUid = @sopInstanceUid
    AND     Watermark = @watermark
    AND     PartitionId = @partitionId

    SELECT @retryCount
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeed
--
-- DESCRIPTION
--     Gets a stream of dicom changes (instance adds and deletes)
--
-- PARAMETERS
--     @limit
--         * Max rows to return
--     @offet
--         * Rows to skip
/***************************************************************************************/
ALTER PROCEDURE dbo.GetChangeFeed (
    @limit      INT,
    @offset     BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  Sequence,
            Timestamp,
            Action,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            PartitionId,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed
    WHERE   Sequence BETWEEN @offset+1 AND @offset+@limit
    ORDER BY Sequence
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatest
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
ALTER PROCEDURE dbo.GetChangeFeedLatest
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            PartitionId,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed
    ORDER BY Sequence DESC
END
GO
