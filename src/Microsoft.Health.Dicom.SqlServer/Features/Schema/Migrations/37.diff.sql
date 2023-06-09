SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    File Property Table
    Stores file properties of a given instance
**************************************************************/
IF NOT EXISTS (
    SELECT *
    FROM sys.tables
    WHERE name = 'FileProperty')
CREATE TABLE dbo.FileProperty (
    InstanceKey BIGINT          NOT NULL,
    Watermark   BIGINT          NOT NULL,
    FilePath    NVARCHAR (4000) NOT NULL,
    ETag        NVARCHAR (4000) NOT NULL
)
WITH (DATA_COMPRESSION = PAGE)
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_FileProperty' AND object_id = OBJECT_ID('dbo.FileProperty'))
CREATE UNIQUE CLUSTERED INDEX IXC_FileProperty
    ON dbo.FileProperty(InstanceKey, Watermark)
    WITH (DATA_COMPRESSION = PAGE, ONLINE = ON)
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatusV6
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
--     @filePath
--         * path to dcm blob file
--     @eTag
--         * eTag of upload blob operation
--
-- RETURN VALUE
--     None
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV37
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @status TINYINT, @maxTagKey INT=NULL, @hasFrameMetadata BIT=0, @path VARCHAR (4000)=NULL, @eTag VARCHAR (4000)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0)
                     FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @instanceKey AS BIGINT;
    UPDATE dbo.Instance
    SET    Status                = @status,
           LastStatusUpdatedDate = @CurrentDate,
           HasFrameMetadata      = @hasFrameMetadata,
           @instanceKey          = InstanceKey
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1;
    IF (@path IS NOT NULL
        AND @eTag IS NOT NULL
        AND @watermark IS NOT NULL)
        INSERT  INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
        VALUES                       (@instanceKey, @watermark, @path, @eTag);
    INSERT  INTO dbo.ChangeFeed (Timestamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES                     (@currentDate, 0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark);
    UPDATE dbo.ChangeFeed
    SET    CurrentWatermark = @watermark
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid;
    COMMIT TRANSACTION;
END
GO

COMMIT TRANSACTION
