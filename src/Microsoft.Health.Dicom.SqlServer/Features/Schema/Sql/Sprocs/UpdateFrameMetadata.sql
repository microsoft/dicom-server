/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateFrameMetadata
--
-- DESCRIPTION
--     Update frame metadata for a list of instances matching the watermark.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
--     @hasFrameMetadata
--         * flag to indicate frame metadata existance
--     @watermarkTableType
--         * The list of watermarks.
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateFrameMetadata
    @partitionKey INT,
    @hasFrameMetadata BIT,
	@watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    UPDATE dbo.Instance
    SET HasFrameMetadata = @hasFrameMetadata
    FROM dbo.Instance i
    JOIN @watermarkTableType input ON i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey

    COMMIT TRANSACTION
END
