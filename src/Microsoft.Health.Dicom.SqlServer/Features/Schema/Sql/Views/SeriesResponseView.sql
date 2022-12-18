
/*************************************************************
    Series View to be used for getting computed column response
**************************************************************/

CREATE VIEW dbo.SeriesResponseView
WITH SCHEMABINDING
AS
SELECT  se.SeriesInstanceUid,
		se.Modality,
		se.PerformedProcedureStepStartDate,
		se.ManufacturerModelName,
		(SELECT COUNT(*) 
		FROM Instance i 
		WHERE se.StudyKey = i.StudyKey
		AND se.SeriesKey = i.SeriesKey
		AND se.PartitionKey = i.PartitionKey) AS NumberofSeriesRelatedInstances
FROM Series se


