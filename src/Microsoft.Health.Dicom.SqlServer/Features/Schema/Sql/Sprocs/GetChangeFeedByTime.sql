/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedByTime
--
-- FIRST SCHEMA VERSION
--     36
--
-- DESCRIPTION
--     Gets a subset of dicom changes within a given time range
--
-- PARAMETERS
--     @startTime
--         * Inclusive timestamp start
--     @endTime
--         * Exclusive timestamp end
--     @offet
--         * Rows to skip
--     @limit
--         * Max rows to return
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedByTime (
    @startTime DATETIMEOFFSET(7),
    @endTime   DATETIMEOFFSET(7),
    @limit     INT,
    @offset    BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    -- As the offset increases, so too does the number of rows read by SQL which may lead
    -- to performance degradation. This can be minimize by smaller time windows as the
    -- Timestamp column is indexed.
    SELECT
        Sequence,
        Timestamp,
        Action,
        PartitionName,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        OriginalWatermark,
        CurrentWatermark
    FROM dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    WHERE c.Timestamp >= @startTime AND c.Timestamp < @endTime
    ORDER BY Timestamp, Sequence
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
