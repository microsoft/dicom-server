
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
		(SELECT CONCAT_WS(',', Modality, NULL) 
		FROM Series se 
		WHERE st.StudyKey = se.StudyKey
		AND st.PartitionKey = se.PartitionKey) AS ModalityInStudy,
		(SELECT COUNT(*) 
		FROM Instance i 
		WHERE st.StudyKey = i.StudyKey
		AND st.PartitionKey = i.PartitionKey) AS NumberofStudyRelatedInstances
FROM Study st

