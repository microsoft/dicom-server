SET XACT_ABORT ON
BEGIN TRANSACTION
/*************************************************************
    Study View to be used for getting computed column response
**************************************************************/
IF EXISTS 
(
    SELECT *
    FROM    sys.views
    WHERE   Name = 'StudyResultView'
)
BEGIN
    EXEC('ALTER VIEW dbo.StudyResultView
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
            (SELECT STRING_AGG(CONVERT(NVARCHAR(max), Modality), '','')
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

COMMIT TRANSACTION


