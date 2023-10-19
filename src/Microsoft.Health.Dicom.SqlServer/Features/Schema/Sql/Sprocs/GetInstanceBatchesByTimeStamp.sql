/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceBatchesByTimeStamp
--
-- DESCRIPTION
--     Divides up the instances into a configurable number of batches filter by timestamp.
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @batchCount
--         * The desired number of batches. Actual number may be smaller.
--     @status
--         * The instance status.
--     @startTimeStamp
--         * The start filter timestamp.
--     @endTimeStamp
--         * The inclusive end filter timestamp.
--     @maxWatermark
--         * The optional inclusive maximum watermark.
--
-- RETURN VALUE
--     The batches as defined by their inclusive minimum and maximum values.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceBatchesByTimeStamp
    @batchSize INT,
    @batchCount INT,
    @status TINYINT,
    @startTimeStamp DATETIMEOFFSET(0),
    @endTimeStamp DATETIMEOFFSET(0),
    @maxWatermark BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        MIN(Watermark) AS MinWatermark,
        MAX(Watermark) AS MaxWatermark
    FROM
    (
        SELECT TOP (@batchSize * @batchCount)
            Watermark,
            (ROW_NUMBER() OVER(ORDER BY Watermark DESC) - 1) / @batchSize AS Batch
        FROM dbo.Instance
        WHERE Watermark <= ISNULL(@maxWatermark, Watermark)
        AND Status = @status
        AND CreatedDate >= @startTimeStamp
        AND CreatedDate <= @endTimeStamp
    ) AS I
    GROUP BY Batch
    ORDER BY Batch ASC
END
