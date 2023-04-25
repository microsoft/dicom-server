SET XACT_ABORT ON

BEGIN TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeed33
--
-- FIRST SCHEMA VERSION
--     33
--
-- DESCRIPTION
--     Gets a stream of dicom changes (instance adds and deletes)
--
-- PARAMETERS
--     @limit
--         * Max rows to return
--     @offet
--         * Rows to skip
--     @startTime
--         * Optional inclusive timestamp start
--     @endTime
--         * Optional exclusive timestamp end
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV33 (
    @startTime DATETIMEOFFSET(7),
    @endTime   DATETIMEOFFSET(7),
    @limit     INT,
    @offset    BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

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
    FROM dbo.ChangeFeed c
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    WHERE c.Timestamp >= @startTime AND c.Timestamp <@endTime
    ORDER BY Sequence ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

COMMIT TRANSACTION
GO
