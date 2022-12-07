/***************************************************************************************/
-- STORED PROCEDURE
--     GetDeletedChangeFeedByWatermarkOrTimeStamp
--
-- DESCRIPTION
--     Gets a stream of deleted dicom changes by watermark or timestamp
--
-- PARAMETERS
--     @batchCount
--         * Max rows to return
--     @timeStamp
--         * Timestamp to 
--     @startWatermark
--         * The inclusive start watermark.
--     @endWatermark
--         * The inclusive end watermark.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetDeletedChangeFeedByWatermarkOrTimeStamp (
    @batchCount INT,
    @timeStamp DATETIME = NULL,
    @startWatermark BIGINT = 0,
    @endWatermark BIGINT = 0)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    IF @timeStamp IS NOT NULL
        SELECT TOP (@batchCount) Sequence,
                Timestamp,
                Action,
                StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                OriginalWatermark,
                CurrentWatermark
        FROM    dbo.ChangeFeed 
        WHERE   Action = 1
                AND TimeStamp >= @timeStamp
        ORDER BY Sequence
    ELSE
        SELECT  Sequence,
                Timestamp,
                Action,
                StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                OriginalWatermark,
                CurrentWatermark
        FROM    dbo.ChangeFeed
        WHERE   Action = 1
                AND Sequence BETWEEN @startWatermark AND @endWatermark
        ORDER BY Sequence
END
