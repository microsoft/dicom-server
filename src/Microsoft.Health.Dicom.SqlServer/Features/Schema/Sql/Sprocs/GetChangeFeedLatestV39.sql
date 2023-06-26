/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestV39
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV39
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
    ORDER BY c.Sequence DESC
END
