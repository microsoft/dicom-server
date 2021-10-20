/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV6
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
    ORDER BY Sequence DESC
END
