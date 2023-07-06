/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestByTimeV39
--
-- FIRST SCHEMA VERSION
--     36
--
-- DESCRIPTION
--     Gets the latest dicom change by timestamp
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestByTimeV39
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            c.Sequence,
            c.Timestamp,
            c.Action,
            p.PartitionName,
            c.StudyInstanceUid,
            c.SeriesInstanceUid,
            c.SopInstanceUid,
            c.OriginalWatermark,
            c.CurrentWatermark,
            c.FilePath
    FROM    dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
        ON p.PartitionKey = c.PartitionKey
    ORDER BY c.Timestamp DESC, c.Sequence DESC
END
