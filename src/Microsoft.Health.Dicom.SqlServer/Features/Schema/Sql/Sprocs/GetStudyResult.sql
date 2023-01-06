/***************************************************************************************/
-- STORED PROCEDURE
--     Get Study level QIDO response
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetStudyResult (
	@partitionKey       INT,
	@watermarkTableType dbo.WatermarkTableType READONLY
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  sv.*
    FROM    dbo.Instance i
    JOIN    @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
    JOIN    dbo.StudyResultView sv  ON  i.StudyKey = sv.StudyKey AND i.PartitionKey = sv.PartitionKey
END
