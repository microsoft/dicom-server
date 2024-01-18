/***************************************************************************************/
-- STORED PROCEDURE
--     GetContentLengthBackFillInstanceBatches
--
-- DESCRIPTION
--     Divides up the instances into a configurable number of batches and only targets instances whose
--     file properties have a content length of 0
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @batchCount
--         * The desired number of batches. Actual number may be smaller.
--
-- RETURN VALUE
--     The batches as defined by their inclusive minimum and maximum values.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetContentLengthBackFillInstanceBatches
    @batchSize INT,
    @batchCount INT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        MIN(Watermark) AS MinWatermark,
        MAX(Watermark) AS MaxWatermark
    FROM
    (
        SELECT TOP (@batchSize * @batchCount)
            I.Watermark,
            (ROW_NUMBER() OVER(ORDER BY I.Watermark DESC) - 1) / @batchSize AS Batch
        FROM dbo.Instance I
        INNER JOIN dbo.FileProperty FP
        ON FP.Watermark = I.Watermark
        WHERE FP.ContentLength = 0
    ) AS I
    GROUP BY Batch
    ORDER BY Batch ASC
END
