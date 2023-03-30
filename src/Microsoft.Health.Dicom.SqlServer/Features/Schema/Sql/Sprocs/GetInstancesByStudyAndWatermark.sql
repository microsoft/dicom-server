/**************************************************************/
--
-- STORED PROCEDURE
--     GetInstancesByStudyAndWatermark
--
-- DESCRIPTION
--     Get instances by given minimum watermark in a study.
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @studyInstanceUid
--         * The study instance UID.
--     @maxWatermark
--         * The optional maxWatermark.
-- RETURN VALUE
--     The instance identifiers.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetInstancesByStudyAndWatermark
    @batchSize INT,
    @partitionKey INT,
    @studyInstanceUid VARCHAR(64),
    @maxWatermark BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT TOP (@batchSize)
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        OriginalWatermark,
        NewWatermark
    FROM dbo.Instance
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND Watermark >= ISNULL(@maxWatermark, Watermark)
        AND Status = 1
    ORDER BY Watermark ASC
END
