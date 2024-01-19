/***************************************************************************************/
-- STORED PROCEDURE
--     Get Series level QIDO response
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetSeriesResult (
	@partitionKey       INT,
	@watermarkTableType dbo.WatermarkTableType READONLY
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  DISTINCT
            i.StudyInstanceUid,
            sv.SeriesInstanceUid,
            sv.Modality,
            sv.PerformedProcedureStepStartDate,
            sv.ManufacturerModelName,
            sv.NumberofSeriesRelatedInstances
    FROM    dbo.Instance i
    JOIN    @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
    JOIN    dbo.SeriesResultView sv  ON  i.StudyKey = sv.StudyKey AND i.SeriesKey = sv.SeriesKey AND i.PartitionKey = sv.PartitionKey
END
