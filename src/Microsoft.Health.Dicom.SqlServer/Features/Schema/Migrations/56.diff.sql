SET XACT_ABORT ON

BEGIN TRANSACTION
GO

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

    SELECT  DISTINCT
            sv.StudyInstanceUid,
            sv.PatientId,
            sv.PatientName,
            sv.ReferringPhysicianName,
            sv.StudyDate,
            sv.StudyDescription,
            sv.AccessionNumber,
            sv.PatientBirthDate,
            sv.ModalitiesInStudy,
            sv.NumberofStudyRelatedInstances
    FROM    dbo.Instance i
    JOIN    @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
    JOIN    dbo.StudyResultView sv  ON  i.StudyKey = sv.StudyKey AND i.PartitionKey = sv.PartitionKey
END
GO

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
GO

COMMIT TRANSACTION
