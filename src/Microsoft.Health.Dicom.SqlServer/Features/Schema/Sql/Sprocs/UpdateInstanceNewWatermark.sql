/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceNewWatermark
--
-- DESCRIPTION
--     Updates a DICOM instance NewWatermark
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceNewWatermark
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION
		
		DECLARE @newWatermark BIGINT

		SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

		UPDATE dbo.Instance
		SET NewWatermark = @newWatermark
		WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND Status = 1

		-- The instance does not exist.
		IF @@ROWCOUNT = 0
			THROW 50404, 'Instance does not exist', 1

    COMMIT TRANSACTION
END
