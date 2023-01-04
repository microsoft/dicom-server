/***************************************************************************************/
-- STORED PROCEDURE
--     Get Series level QIDO response
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetSeriesAttributes (
	@partitionKey       INT,
	@watermarkTableType dbo.WatermarkTableType READONLY
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  sv.*
    FROM    dbo.SeriesResponseView sv
    JOIN    dbo.Instance i
	ON      i.PartitionKey = sv.PartitionKey
    AND     i.PartitionKey = @partitionKey
    AND     i.StudyKey = sv.StudyKey
    AND     i.SeriesKey = sv.SeriesKey
	JOIN    @watermarkTableType input
	ON      i.Watermark = input.Watermark
END
