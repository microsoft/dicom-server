/***************************************************************************************/
-- STORED PROCEDURE
--     IncrementDeletedInstanceRetryV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Increments the retryCount of and retryAfter of a deleted instance
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
--     @cleanupAfter
--         * The next date time to attempt cleanup
--
-- RETURN VALUE
--     The retry count.
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IncrementDeletedInstanceRetryV6(
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @cleanupAfter       DATETIMEOFFSET(0)
)
AS
    SET NOCOUNT ON

    DECLARE @retryCount INT

    UPDATE  dbo.DeletedInstance
    SET     @retryCount = RetryCount = RetryCount + 1,
            CleanupAfter = @cleanupAfter
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = @seriesInstanceUid
        AND     SopInstanceUid = @sopInstanceUid
        AND     Watermark = @watermark

    SELECT @retryCount
