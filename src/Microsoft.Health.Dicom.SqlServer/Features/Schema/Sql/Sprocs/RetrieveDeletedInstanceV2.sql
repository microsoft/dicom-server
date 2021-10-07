/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstanceV2
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
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV2
    @count          INT,
    @maxRetries     INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT  TOP (@count) PartitionName, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM    dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
END
