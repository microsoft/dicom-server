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
            f.FilePath
    FROM    dbo.ChangeFeed c WITH (HOLDLOCK)
    INNER JOIN dbo.Partition p
        ON p.PartitionKey = c.PartitionKey
    -- Left join as instance may have been deleted
    LEFT OUTER JOIN dbo.Instance AS i
        ON i.StudyInstanceUid = c.StudyInstanceUid
        AND i.SeriesInstanceUid = c.SeriesInstanceUid
        AND i.SopInstanceUid = c.SopInstanceUid
    -- Left join as instance and property may have been deleted or we never inserted property for instance when not 
    -- using external store
	LEFT OUTER JOIN dbo.FileProperty as f
		ON f.InstanceKey = i.InstanceKey
    ORDER BY c.Timestamp DESC, c.Sequence DESC
END
