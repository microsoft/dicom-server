/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestByTime
--
-- FIRST SCHEMA VERSION
--     35
--
-- DESCRIPTION
--     Gets the latest dicom change by timestamp
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestByTime
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    ORDER BY Timestamp DESC, Sequence DESC
END
