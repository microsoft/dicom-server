/***************************************************************************************/
-- STORED PROCEDURE
--     GetMaxDeletedChangeFeedWatermark
--
-- DESCRIPTION
--     Gets max watermark for deleted changefeed records
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
CREATE OR ALTER PROCEDURE dbo.GetMaxDeletedChangeFeedWatermark(@timeStamp DATETIME)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT MAX(Sequence)
    FROM    dbo.ChangeFeed 
    WHERE   Action = 1 -- Deleted
            AND TimeStamp >= @timeStamp
END
