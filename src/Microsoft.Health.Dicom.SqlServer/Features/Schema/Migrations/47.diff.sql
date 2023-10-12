SET XACT_ABORT ON

BEGIN TRANSACTION
/*************************************************************
    Table Updates
**************************************************************/
      
/*************************************************************
    DeletedInstance Table
    Add FilePath and ETag nullable columns
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   (NAME = 'FilePath')
        AND Object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    ALTER TABLE dbo.DeletedInstance
    ADD FilePath NVARCHAR(4000) NULL,
        ETag NVARCHAR(4000) NULL
END
GO

/*************************************************************
    SPROC Updates
**************************************************************/

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
-- CHANGE SUMMARY
-- This sproc now returns values
/***************************************************************************************/

CREATE OR ALTER PROCEDURE dbo.DeleteInstanceV6
    @cleanupAfter DATETIMEOFFSET (0), @createdStatus TINYINT, @partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64)=NULL, @sopInstanceUid VARCHAR (64)=NULL
    AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
BEGIN TRANSACTION;
    DECLARE @deletedInstances AS TABLE (
        PartitionKey      INT            ,
        StudyInstanceUid  VARCHAR (64)   ,
        SeriesInstanceUid VARCHAR (64)   ,
        SopInstanceUid    VARCHAR (64)   ,
        Status            TINYINT        ,
        Watermark         BIGINT         ,
        InstanceKey       INT            ,
        OriginalWatermark BIGINT         ,
        FilePath          NVARCHAR (4000) NULL,
        ETag              NVARCHAR (4000) NULL);
    DECLARE @studyKey AS BIGINT;
    DECLARE @seriesKey AS BIGINT;
    DECLARE @instanceKey AS BIGINT;
    DECLARE @deletedDate AS DATETIME2 = SYSUTCDATETIME();
    DECLARE @imageResourceType AS TINYINT = 0;
SELECT @studyKey = StudyKey,
       @seriesKey = CASE @seriesInstanceUid WHEN NULL THEN NULL ELSE SeriesKey END,
       @instanceKey = CASE @sopInstanceUid WHEN NULL THEN NULL ELSE InstanceKey END
FROM   dbo.Instance
WHERE  PartitionKey = @partitionKey
  AND StudyInstanceUid = @studyInstanceUid
  AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
  AND SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid);
DELETE dbo.Instance
    OUTPUT deleted.PartitionKey, deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark, deleted.InstanceKey, deleted.OriginalWatermark, FP.FilePath, FP.ETag INTO @deletedInstances
    FROM   dbo.Instance AS i
           LEFT OUTER JOIN
           dbo.FileProperty AS FP
           ON i.InstanceKey = FP.InstanceKey
              AND i.Watermark = FP.Watermark
    WHERE  i.PartitionKey = @partitionKey
           AND i.StudyInstanceUid = @studyInstanceUid
           AND i.SeriesInstanceUid = ISNULL(@seriesInstanceUid, i.SeriesInstanceUid)
           AND i.SopInstanceUid = ISNULL(@sopInstanceUid, i.SopInstanceUid);
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance not found', 1;
    DELETE FP
    FROM   dbo.FileProperty AS FP
           INNER JOIN
           @deletedInstances AS DI
           ON DI.InstanceKey = FP.InstanceKey
              AND DI.Watermark = FP.Watermark;
    DECLARE @deletedTags AS TABLE (
        TagKey BIGINT);
    DELETE XQTE
    OUTPUT deleted.TagKey INTO @deletedTags
    FROM   dbo.ExtendedQueryTagError AS XQTE
           INNER JOIN
           @deletedInstances AS DI
           ON XQTE.Watermark = DI.Watermark;
    IF EXISTS (SELECT *
               FROM   @deletedTags)
BEGIN
            DECLARE @deletedTagCounts AS TABLE (
                TagKey     BIGINT,
                ErrorCount INT   );
INSERT INTO @deletedTagCounts (TagKey, ErrorCount)
SELECT   TagKey,
         COUNT(1)
FROM     @deletedTags
GROUP BY TagKey;
UPDATE XQT
SET    XQT.ErrorCount = XQT.ErrorCount - DTC.ErrorCount
    FROM   dbo.ExtendedQueryTag AS XQT
                   INNER JOIN
                   @deletedTagCounts AS DTC
ON XQT.TagKey = DTC.TagKey;
END
    DELETE dbo.ExtendedQueryTagString
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagLong
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagDouble
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagDateTime
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
    DELETE dbo.ExtendedQueryTagPersonName
    WHERE  SopInstanceKey1 = @studyKey
           AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
           AND SopInstanceKey3 = ISNULL(@instanceKey, SopInstanceKey3)
           AND PartitionKey = @partitionKey
           AND ResourceType = @imageResourceType;
INSERT INTO dbo.DeletedInstance (PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter, OriginalWatermark, FilePath, ETag)
SELECT PartitionKey,
       StudyInstanceUid,
       SeriesInstanceUid,
       SopInstanceUid,
       Watermark,
       @deletedDate,
       0,
       @cleanupAfter,
       OriginalWatermark,
       FilePath,
       ETag
FROM   @deletedInstances;
IF NOT EXISTS (SELECT *
                   FROM   dbo.Instance WITH (HOLDLOCK, UPDLOCK)
                   WHERE  PartitionKey = @partitionKey
                          AND StudyKey = @studyKey
                          AND SeriesKey = ISNULL(@seriesKey, SeriesKey))
BEGIN
            DELETE dbo.Series
            WHERE  StudyKey = @studyKey
                   AND SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
                   AND PartitionKey = @partitionKey;
            DELETE dbo.ExtendedQueryTagString
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagLong
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDouble
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDateTime
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagPersonName
            WHERE  SopInstanceKey1 = @studyKey
                   AND SopInstanceKey2 = ISNULL(@seriesKey, SopInstanceKey2)
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
END
    IF NOT EXISTS (SELECT *
                   FROM   dbo.Series WITH (HOLDLOCK, UPDLOCK)
                   WHERE  Studykey = @studyKey
                          AND PartitionKey = @partitionKey)
BEGIN
            DELETE dbo.Study
            WHERE  StudyKey = @studyKey
                   AND PartitionKey = @partitionKey;
            DELETE dbo.ExtendedQueryTagString
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagLong
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDouble
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagDateTime
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
            DELETE dbo.ExtendedQueryTagPersonName
            WHERE  SopInstanceKey1 = @studyKey
                   AND PartitionKey = @partitionKey
                   AND ResourceType = @imageResourceType;
END
INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
SELECT 1,
       DI.PartitionKey,
       DI.StudyInstanceUid,
       DI.SeriesInstanceUid,
       DI.SopInstanceUid,
       DI.Watermark
FROM   @deletedInstances AS DI
WHERE  Status = @createdStatus;
UPDATE CF
SET    CF.CurrentWatermark = NULL
    FROM   dbo.ChangeFeed AS CF WITH (FORCESEEK)
    INNER JOIN
    @deletedInstances AS DI
ON CF.PartitionKey = DI.PartitionKey
    AND CF.StudyInstanceUid = DI.StudyInstanceUid
    AND CF.SeriesInstanceUid = DI.SeriesInstanceUid
    AND CF.SopInstanceUid = DI.SopInstanceUid;
COMMIT TRANSACTION;
SELECT d.Watermark,
       d.partitionKey,
       d.studyInstanceUid,
       d.seriesInstanceUid,
       d.sopInstanceUid
FROM   @deletedInstances AS d;
END
GO

COMMIT TRANSACTION

/***************************************************************************************/
-- Index Updates
/***************************************************************************************/

/***************************************************************************************/
-- IX_DeletedInstance_RetryCount_CleanupAfter now has FilePath and ETag included in return
/***************************************************************************************/


IF EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_DeletedInstance_RetryCount_CleanupAfter' AND object_id = OBJECT_ID('dbo.DeletedInstance'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeletedInstance_RetryCount_CleanupAfter
        ON dbo.DeletedInstance(RetryCount, CleanupAfter)
        INCLUDE(
                  PartitionKey,
                  StudyInstanceUid,
                  SeriesInstanceUid,
                  SopInstanceUid,
                  Watermark,
                  OriginalWatermark,
                  FilePath,
                  ETag) 
        WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING = ON, ONLINE = ON)
END
