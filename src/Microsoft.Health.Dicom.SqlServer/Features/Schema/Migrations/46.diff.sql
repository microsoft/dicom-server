SET XACT_ABORT ON

BEGIN TRANSACTION
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     BeginUpdateInstance
--
-- DESCRIPTION
--     Updates a DICOM instance NewWatermark for a given study
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @studyInstanceUid
--         * The study instance uid.
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstanceV46
    @partitionKey INT, @studyInstanceUid VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.Instance
    SET    NewWatermark =  NEXT VALUE FOR dbo.WatermarkSequence
    WHERE  PartitionKey = @partitionKey
      AND StudyInstanceUid = @studyInstanceUid
      AND Status = 1;
    COMMIT TRANSACTION;
    SELECT i.StudyInstanceUid,
           i.SeriesInstanceUid,
           i.SopInstanceUid,
           i.Watermark,
           i.TransferSyntaxUid,
           i.HasFrameMetadata,
           i.OriginalWatermark,
           i.NewWatermark,
           f.FilePath,
           f.ETag
    FROM   dbo.Instance AS i
               LEFT OUTER JOIN
           dbo.FileProperty AS f
           ON f.InstanceKey = i.InstanceKey
               AND f.Watermark = i.Watermark
    WHERE  i.PartitionKey = @partitionKey
      AND i.StudyInstanceUid = @studyInstanceUid
      AND i.Status = 1;
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceWithPropertiesV46
--
-- FIRST SCHEMA VERSION
--     32
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level with additional instance properties
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV46 (
    @validStatus        TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL
    )
    AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON


SELECT  i.StudyInstanceUid,
        i.SeriesInstanceUid,
        i.SopInstanceUid,
        i.Watermark,
        i.TransferSyntaxUid,
        i.HasFrameMetadata,
        i.OriginalWatermark,
        i.NewWatermark,
        f.FilePath,
        f.ETag
FROM    dbo.Instance as i
            LEFT OUTER JOIN
        dbo.FileProperty AS f
        ON f.InstanceKey = i.InstanceKey
            AND f.Watermark = i.Watermark
WHERE i.PartitionKey            = @partitionKey
  AND i.StudyInstanceUid    = @studyInstanceUid
  AND i.SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
  AND i.SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
  AND i.Status              = @validStatus

END
GO
COMMIT TRANSACTION
