/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Removes the specified instance(s) and places them in the DeletedInstance table for later removal, along with
--      associated blob file path and etag
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
--
-- RETURNS
--     studyInstanceUid
--         * The study instance UID.
--     seriesInstanceUid
--         * The series instance UID.
--     sopInstanceUid
--         * The SOP instance UID.
--     watermark
--          * Version of the instance being deleted
--     partitionKey
--          * partition within which instance is being deleted
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
         InstanceKey INT,
         OriginalWatermark BIGINT,
         FilePath NVARCHAR (4000) NULL,
         ETag NVARCHAR (4000) NULL)

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
    
    -- By joining to file properties, this will capture both original and new files for any instances
    -- that had gone through the update operation with external store enabled. We still need original watermark
    -- to delete original files where file properties were not used
    DELETE  dbo.Instance
    OUTPUT deleted.PartitionKey, deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark, deleted.InstanceKey, deleted.OriginalWatermark, FP.FilePath, FP.ETag
        INTO @deletedInstances
    FROM dbo.Instance as i
        LEFT OUTER JOIN dbo.FileProperty as FP
        ON i.InstanceKey = FP.InstanceKey
        AND i.Watermark = FP.Watermark
    WHERE   i.PartitionKey = @partitionKey
        AND     i.StudyInstanceUid = @studyInstanceUid
        AND     i.SeriesInstanceUid = ISNULL(@seriesInstanceUid, i.SeriesInstanceUid)
        AND     i.SopInstanceUid = ISNULL(@sopInstanceUid, i.SopInstanceUid)

    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance not found', 1
        
    -- Delete FileProperties of instance
    DELETE FP
    FROM dbo.FileProperty as FP
    INNER JOIN @deletedInstances AS DI
    ON DI.InstanceKey = FP.InstanceKey
    AND DI.Watermark = FP.Watermark

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
    (PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter, OriginalWatermark, FilePath, ETag)
    SELECT PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, @deletedDate, 0 , @cleanupAfter, OriginalWatermark, FilePath, ETag
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
    SELECT 1, DI.PartitionKey, DI.StudyInstanceUid, DI.SeriesInstanceUid, DI.SopInstanceUid, DI.Watermark
    FROM @deletedInstances as DI
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

    SELECT d.Watermark,
           d.partitionKey,
           d.studyInstanceUid,
           d.seriesInstanceUid,
           d.sopInstanceUid
    FROM   @deletedInstances AS d
END
