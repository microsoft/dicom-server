/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstanceV47
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Retrieves deleted instances where the cleanupAfter is less than the current date in and the retry count hasn't been exceeded
--
-- PARAMETERS
--     @count
--         * The number of entries to return
--     @maxRetries
--         * The maximum number of times to retry a cleanup
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV47
    @count          INT,
    @maxRetries     INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT  TOP (@count) p.PartitionName, d.PartitionKey, d.StudyInstanceUid, d.SeriesInstanceUid, d.SopInstanceUid, d.Watermark, d.OriginalWatermark, d.FilePath, d.ETag
    FROM    dbo.DeletedInstance as d WITH (UPDLOCK, READPAST)
    INNER JOIN dbo.Partition as p
        ON p.PartitionKey = d.PartitionKey
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
END
