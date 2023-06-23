/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedV39
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets a stream of dicom changes (instance adds and deletes)
--
-- PARAMETERS
--     @limit
--         * Max rows to return
--     @offet
--         * Rows to skip
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV39 (
    @limit      INT,
    @offset     BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  c.Sequence,
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
    WHERE   c.Sequence BETWEEN @offset+1 AND @offset+@limit
    ORDER BY c.Sequence
END
