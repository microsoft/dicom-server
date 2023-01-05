/*************************************************************
    Study View to be used for getting computed column response
**************************************************************/

CREATE VIEW dbo.StudyResponseView
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
		(SELECT STRING_AGG(Modality, ',')
		FROM dbo.Series se 
		WHERE st.StudyKey = se.StudyKey
		AND st.PartitionKey = se.PartitionKey) AS ModalitiesInStudy,
		(SELECT SUM(1) 
		FROM dbo.Instance i 
		WHERE st.StudyKey = i.StudyKey
		AND st.PartitionKey = i.PartitionKey) AS NumberofStudyRelatedInstances,
        st.PartitionKey,
        st.StudyKey
FROM dbo.Study st
