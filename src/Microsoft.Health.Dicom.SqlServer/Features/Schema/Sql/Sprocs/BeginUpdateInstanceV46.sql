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
-- RETURNS
-- The updated instance and its file properties.
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
