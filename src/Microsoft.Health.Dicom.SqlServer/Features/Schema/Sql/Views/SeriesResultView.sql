/*************************************************************
    Series View to be used for getting computed column response
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.views
    WHERE   Name = 'SeriesResultView'
)
BEGIN
    EXEC('CREATE VIEW dbo.SeriesResultView
    WITH SCHEMABINDING
    AS
    SELECT  se.SeriesInstanceUid,
            se.Modality,
            se.PerformedProcedureStepStartDate,
            se.ManufacturerModelName,
            (SELECT SUM(1)
            FROM dbo.Instance i 
            WHERE se.PartitionKey = i.PartitionKey
            AND se.StudyKey = i.StudyKey
            AND se.SeriesKey = i.SeriesKey) AS NumberofSeriesRelatedInstances,
            se.PartitionKey,
            se.StudyKey,
            se.SeriesKey
    FROM dbo.Series se')
END
