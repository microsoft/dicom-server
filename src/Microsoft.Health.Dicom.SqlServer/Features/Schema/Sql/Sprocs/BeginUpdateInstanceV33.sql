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
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstanceV33
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION
        
        UPDATE dbo.Instance
        SET NewWatermark = NEXT VALUE FOR dbo.WatermarkSequence
        WHERE PartitionKey            = @partitionKey
              AND StudyInstanceUid    = @studyInstanceUid
              AND Status              = 1

    COMMIT TRANSACTION

    SELECT StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        TransferSyntaxUid,
        HasFrameMetadata,
        OriginalWatermark,
        NewWatermark
    FROM dbo.Instance
    WHERE PartitionKey            = @partitionKey
          AND StudyInstanceUid    = @studyInstanceUid
          AND Status              = 1
END
