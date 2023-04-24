/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     BeginUpdateInstance
--
-- DESCRIPTION
--     Updates a DICOM instance NewWatermark
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @watermarkTableType
--         * The SOP instance watermark.
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstance
    @partitionKey       INT,
    @watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION
        
        UPDATE i
        SET NewWatermark = NEXT VALUE FOR dbo.WatermarkSequence
        FROM dbo.Instance i
        JOIN @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
        WHERE Status = 1

    COMMIT TRANSACTION

    SELECT StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        i.Watermark,
        TransferSyntaxUid,
        HasFrameMetadata,
        OriginalWatermark,
        NewWatermark
    FROM dbo.Instance i
    JOIN @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
    WHERE Status = 1
END
