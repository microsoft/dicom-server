/***************************************************************************************/
-- STORED PROCEDURE
--     Get Study level QIDO response
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetStudyAttributes (
	@partitionKey       INT,
	@watermarkTableType dbo.WatermarkTableType READONLY
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  sv.*
    FROM    dbo.StudyResponseView sv
    JOIN    dbo.Instance i
	ON      i.PartitionKey = sv.PartitionKey
	AND     i.PartitionKey = @partitionKey
    AND     i.StudyKey = sv.StudyKey
	JOIN    @watermarkTableType input
    ON      i.Watermark = input.Watermark
END
