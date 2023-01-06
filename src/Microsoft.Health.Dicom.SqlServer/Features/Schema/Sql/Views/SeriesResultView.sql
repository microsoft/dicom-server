/*************************************************************
    Series View to be used for getting computed column response
**************************************************************/

CREATE VIEW dbo.SeriesResultView
WITH SCHEMABINDING
AS
SELECT  se.SeriesInstanceUid,
		se.Modality,
		se.PerformedProcedureStepStartDate,
		se.ManufacturerModelName,
		(SELECT SUM(1)
		FROM dbo.Instance i 
		WHERE se.StudyKey = i.StudyKey
		AND se.SeriesKey = i.SeriesKey
		AND se.PartitionKey = i.PartitionKey) AS NumberofSeriesRelatedInstances,
        se.PartitionKey,
        se.StudyKey,
        se.SeriesKey
FROM dbo.Series se
