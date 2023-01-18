SET XACT_ABORT ON
BEGIN TRANSACTION
/*************************************************************
    Study View to be used for getting computed column response
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.views
    WHERE   Name = 'StudyResultView'
)
BEGIN
    EXEC('CREATE VIEW dbo.StudyResultView
    WITH SCHEMABINDING
    AS
    SELECT  st.StudyInstanceUid,
            st.PatientId,
            st.PatientName,
            st.ReferringPhysicianName,
            st.StudyDate,
            st.StudyDescription,
            st.AccessionNumber,
            st.PatientBirthDate,
            (SELECT STRING_AGG(Modality, '','')
            FROM dbo.Series se 
            WHERE st.StudyKey = se.StudyKey
            AND st.PartitionKey = se.PartitionKey) AS ModalitiesInStudy,
            (SELECT SUM(1) 
            FROM dbo.Instance i 
            WHERE st.StudyKey = i.StudyKey) AS NumberofStudyRelatedInstances,
            st.PartitionKey,
            st.StudyKey
    FROM dbo.Study st')
END
GO
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
            WHERE se.SeriesKey = i.SeriesKey) AS NumberofSeriesRelatedInstances,
            se.PartitionKey,
            se.StudyKey,
            se.SeriesKey
    FROM dbo.Series se')
END
GO
/*************************************************************
    Table value param to get query response
*************************************************************/
IF TYPE_ID(N'WatermarkTableType') IS NULL
BEGIN
CREATE TYPE dbo.WatermarkTableType AS TABLE
(
   Watermark                  BIGINT
)
END
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

    SELECT  sv.StudyInstanceUid,
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

    SELECT  i.StudyInstanceUid,
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

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_Watermark'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_Watermark on dbo.Instance
    (
        PartitionKey,
        Watermark
    )
    INCLUDE
    (
        StudyKey,
        SeriesKey,
        StudyInstanceUid
    )
    WITH (DATA_COMPRESSION = PAGE)
END

