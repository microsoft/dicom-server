SET XACT_ABORT ON

CREATE UNIQUE CLUSTERED INDEX IXC_ChangeFeed ON dbo.ChangeFeed
(
    Timestamp,
    Sequence
) WITH (DROP_EXISTING=ON, ONLINE=ON)

IF NOT EXISTS
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ChangeFeed_Sequence'
        AND Object_id = OBJECT_ID('dbo.ChangeFeed')
)
BEGIN
    -- For use with the V1 APIs that use Sequence
    CREATE NONCLUSTERED INDEX IX_ChangeFeed_Sequence ON dbo.ChangeFeed
    (
        Sequence
    ) WITH (DATA_COMPRESSION = PAGE, ONLINE=ON)
END

BEGIN TRANSACTION
GO

IF NOT EXISTS
(
    SELECT *
    FROM sys.all_columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.default_constraints d ON c.default_object_id = d.object_id
    WHERE t.name = 'ChangeFeed' AND c.name = 'Timestamp'
)
BEGIN

    ALTER TABLE dbo.ChangeFeed ADD DEFAULT SYSDATETIMEOFFSET() FOR Timestamp

END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Removes the specified instance(s) and places them in the DeletedInstance table for later removal
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
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
CREATE OR ALTER PROCEDURE dbo.DeleteInstanceV6
    @cleanupAfter       DATETIMEOFFSET(0),
    @createdStatus      TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

    DECLARE @deletedInstances AS TABLE
           (PartitionKey INT,
            StudyInstanceUid VARCHAR(64),
            SeriesInstanceUid VARCHAR(64),
            SopInstanceUid VARCHAR(64),
            Status TINYINT,
            Watermark BIGINT,
            OriginalWatermark BIGINT)

    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT
    DECLARE @deletedDate DATETIME2 = SYSUTCDATETIME()
    DECLARE @imageResourceType AS TINYINT = 0

    -- Get the study, series and instance PK
    SELECT  @studyKey = StudyKey,
    @seriesKey = CASE @seriesInstanceUid WHEN NULL THEN NULL ELSE SeriesKey END,
    @instanceKey = CASE @sopInstanceUid WHEN NULL THEN NULL ELSE InstanceKey END
    FROM    dbo.Instance
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    -- Delete the instance and insert the details into DeletedInstance and ChangeFeed
    DELETE  dbo.Instance
        OUTPUT deleted.PartitionKey, deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark, deleted.OriginalWatermark
        INTO @deletedInstances
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance not found', 1

    -- Deleting tag errors
    DECLARE @deletedTags AS TABLE
    (
        TagKey BIGINT
    )
    DELETE XQTE
        OUTPUT deleted.TagKey
        INTO @deletedTags
    FROM dbo.ExtendedQueryTagError as XQTE
    INNER JOIN @deletedInstances AS DI
    ON XQTE.Watermark = DI.Watermark

    -- Update error count
    IF EXISTS (SELECT * FROM @deletedTags)
    BEGIN
        DECLARE @deletedTagCounts AS TABLE
        (
            TagKey BIGINT,
            ErrorCount INT
        )

        -- Calculate error count
        INSERT INTO @deletedTagCounts
            (TagKey, ErrorCount)
        SELECT TagKey, COUNT(1)
        FROM @deletedTags
        GROUP BY TagKey

        UPDATE XQT
        SET XQT.ErrorCount = XQT.ErrorCount - DTC.ErrorCount
        FROM dbo.ExtendedQueryTag AS XQT
        INNER JOIN @deletedTagCounts AS DTC
        ON XQT.TagKey = DTC.TagKey
    END

    -- Deleting indexed instance tags
    DELETE
    FROM    dbo.ExtendedQueryTagString
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey
    AND     ResourceType = @imageResourceType

    DELETE
    FROM    dbo.ExtendedQueryTagLong
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey
    AND     ResourceType = @imageResourceType

    DELETE
    FROM    dbo.ExtendedQueryTagDouble
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey
    AND     ResourceType = @imageResourceType

    DELETE
    FROM    dbo.ExtendedQueryTagDateTime
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey
    AND     ResourceType = @imageResourceType

    DELETE
    FROM    dbo.ExtendedQueryTagPersonName
    WHERE   SopInstanceKey1 = @studyKey
    AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
    AND     SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
    AND     PartitionKey = @partitionKey
    AND     ResourceType = @imageResourceType

    INSERT INTO dbo.DeletedInstance
    (PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter, OriginalWatermark)
    SELECT PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, @deletedDate, 0 , @cleanupAfter, OriginalWatermark
    FROM @deletedInstances

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance WITH(HOLDLOCK, UPDLOCK)
                    WHERE   PartitionKey = @partitionKey
                    AND     StudyKey = @studyKey
                    AND     SeriesKey = ISNULL(@seriesKey, SeriesKey))
    BEGIN
        DELETE
        FROM    dbo.Series
        WHERE   StudyKey = @studyKey
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
        AND     PartitionKey = @partitionKey

        -- Deleting indexed series tags
        DELETE
        FROM    dbo.ExtendedQueryTagString
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   SopInstanceKey1 = @studyKey
        AND     SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType
    END

    -- If we've removing the series, see if it's the last for a study and if so, remove the study
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Series WITH(HOLDLOCK, UPDLOCK)
                    WHERE   Studykey = @studyKey
                    AND     PartitionKey = @partitionKey)
    BEGIN
        DELETE
        FROM    dbo.Study
        WHERE   StudyKey = @studyKey
        AND     PartitionKey = @partitionKey

        -- Deleting indexed study tags
        DELETE
        FROM    dbo.ExtendedQueryTagString
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagLong
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagDouble
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagDateTime
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType

        DELETE
        FROM    dbo.ExtendedQueryTagPersonName
        WHERE   SopInstanceKey1 = @studyKey
        AND     PartitionKey = @partitionKey
        AND     ResourceType = @imageResourceType
    END

    INSERT INTO dbo.ChangeFeed
    (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 1, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM @deletedInstances
    WHERE Status = @createdStatus

    UPDATE CF
    SET CF.CurrentWatermark = NULL
    FROM dbo.ChangeFeed AS CF WITH(FORCESEEK)
    JOIN @deletedInstances AS DI
    ON CF.PartitionKey = DI.PartitionKey
        AND CF.StudyInstanceUid = DI.StudyInstanceUid
        AND CF.SeriesInstanceUid = DI.SeriesInstanceUid
        AND CF.SopInstanceUid = DI.SopInstanceUid

    COMMIT TRANSACTION
END
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     EndUpdateInstance
--
-- DESCRIPTION
--     Bulk update all instances in a study, creates new entry in changefeed.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
--     @studyInstanceUid
--         * The study instance UID.
--     @patientId
--         * The Id of the patient.
--     @patientName
--         * The name of the patient.
--     @patientBirthDate
--         * The patient's birth date.
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstance
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @patientId                          NVARCHAR(64) = NULL,
    @patientName                        NVARCHAR(325) = NULL,
    @patientBirthDate                   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
        DECLARE @updatedInstances AS TABLE
               (PartitionKey INT,
                StudyInstanceUid VARCHAR(64),
                SeriesInstanceUid VARCHAR(64),
                SopInstanceUid VARCHAR(64),
                Watermark BIGINT)

        DELETE FROM @updatedInstances

        UPDATE dbo.Instance
        SET LastStatusUpdatedDate = @currentDate,
            OriginalWatermark = ISNULL(OriginalWatermark, Watermark),
            Watermark = NewWatermark,
            NewWatermark = NULL
        OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.NewWatermark INTO @updatedInstances
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid
            AND Status = 1
            AND NewWatermark IS NOT NULL

        -- Only updating patient information in a study
        UPDATE dbo.Study
        SET PatientId = ISNULL(@patientId, PatientId), 
            PatientName = ISNULL(@patientName, PatientName), 
            PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate)
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid 

        -- The study does not exist. May be deleted
        IF @@ROWCOUNT = 0
            THROW 50404, 'Study does not exist', 1

        -- Insert into change feed table for update action type
        INSERT INTO dbo.ChangeFeed
        (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
        SELECT 2, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
        FROM @updatedInstances

        -- Update existing instance currentWatermark to latest
        UPDATE C
        SET CurrentWatermark = U.Watermark
        FROM dbo.ChangeFeed C
        JOIN @updatedInstances U
        ON C.PartitionKey = U.PartitionKey
            AND C.StudyInstanceUid = U.StudyInstanceUid
            AND C.SeriesInstanceUid = U.SeriesInstanceUid
            AND C.SopInstanceUid = U.SopInstanceUid

    COMMIT TRANSACTION
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedV6
--
-- FIRST SCHEMA VERSION
--     6
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
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV6 (
    @limit      INT,
    @offset     BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    WHERE   Sequence BETWEEN @offset+1 AND @offset+@limit
    ORDER BY Sequence
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedByTime
--
-- FIRST SCHEMA VERSION
--     36
--
-- DESCRIPTION
--     Gets a subset of dicom changes within a given time range
--
-- PARAMETERS
--     @startTime
--         * Inclusive timestamp start
--     @endTime
--         * Exclusive timestamp end
--     @offet
--         * Rows to skip
--     @limit
--         * Max rows to return
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedByTime (
    @startTime DATETIMEOFFSET(7),
    @endTime   DATETIMEOFFSET(7),
    @limit     INT,
    @offset    BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    -- As the offset increases, so too does the number of rows read by SQL which may lead
    -- to performance degradation. This can be minimize by smaller time windows as the
    -- Timestamp column is indexed.
    SELECT
        Sequence,
        Timestamp,
        Action,
        PartitionName,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        OriginalWatermark,
        CurrentWatermark
    FROM dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    WHERE c.Timestamp >= @startTime AND c.Timestamp < @endTime
    ORDER BY Timestamp, Sequence
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV6
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    ORDER BY Sequence DESC
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestByTime
--
-- FIRST SCHEMA VERSION
--     36
--
-- DESCRIPTION
--     Gets the latest dicom change by timestamp
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestByTime
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    ORDER BY Timestamp DESC, Sequence DESC
END
GO

/***************************************************************************************/
--STORED PROCEDURE
--     GetExtendedQueryTagsV36
--
-- FIRST SCHEMA VERSION
--     36
--
-- DESCRIPTION
--     Gets a possibly paginated set of query tags as indicated by the parameters
--
-- PARAMETERS
--     @limit
--         * The maximum number of results to retrieve.
--     @offset
--         * The offset from which to retrieve paginated results.
--
-- RETURN VALUE
--     The set of query tags.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsV36
    @limit  INT,
    @offset BIGINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM dbo.ExtendedQueryTag AS XQT
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    ORDER BY XQT.TagKey ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagErrorsV36
--
-- FIRST SCHEMA VERSION
--     36
--
-- DESCRIPTION
--     Gets the extended query tag errors by tag path.
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag for which we retrieve error(s).
--     @limit
--         * The maximum number of results to retrieve.
--     @offset
--         * The offset from which to retrieve paginated results.
--
-- RETURN VALUE
--     The tag error fields and the corresponding instance UIDs.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrorsV36
    @tagPath VARCHAR(64),
    @limit   INT,
    @offset  BIGINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @tagKey INT
    SELECT @tagKey = TagKey
    FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)
    WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

    -- Check existence
    IF (@@ROWCOUNT = 0)
        THROW 50404, 'extended query tag not found', 1

    SELECT
        TagKey,
        ErrorCode,
        CreatedTime,
        PartitionName,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    FROM dbo.ExtendedQueryTagError AS XQTE
    INNER JOIN dbo.Instance AS I
    ON XQTE.Watermark = I.Watermark
    INNER JOIN dbo.Partition P
    ON P.PartitionKey = I.PartitionKey
    WHERE XQTE.TagKey = @tagKey
    ORDER BY CreatedTime ASC, XQTE.Watermark ASC, TagKey ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatusV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Updates a DICOM instance status, which allows for consistency during indexing.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark.
--     @status
--         * The new status to update to.
--     @maxTagKey
--         * Optional max ExtendedQueryTag key
--     @hasFrameMetadata
--         * Optional flag to indicate frame metadata existance
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV6
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @status             TINYINT,
    @maxTagKey          INT = NULL,
    @hasFrameMetadata   BIT = 0
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    -- This check ensures the client is not potentially missing 1 or more query tags that may need to be indexed.
    -- Note that if @maxTagKey is NULL, < will always return UNKNOWN.
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate, HasFrameMetadata = @hasFrameMetadata
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND Watermark = @watermark

    -- The instance does not exist. Perhaps it was deleted?
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES
        (0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid  = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION
GO
