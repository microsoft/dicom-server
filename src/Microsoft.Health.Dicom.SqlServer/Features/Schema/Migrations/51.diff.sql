SET XACT_ABORT ON

/*************************************************************
    View Updates
    Drop the view to alter the table without any issues
**************************************************************/

IF EXISTS (SELECT *
               FROM   sys.views
               WHERE  Name = 'StudyResultView')
BEGIN
    DROP VIEW dbo.StudyResultView
END
GO

/*************************************************************
    Table Updates
**************************************************************/

/*************************************************************
    Study Table
    Alter PatientId column to accept null value
**************************************************************/

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'PatientId'
        AND Object_id = OBJECT_ID('dbo.Study')
        AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Study
    ALTER COLUMN PatientId NVARCHAR(64) NULL
END
GO

/*************************************************************
    View Updates
    Recreate the view after altering the table
**************************************************************/

IF NOT EXISTS (SELECT *
               FROM   sys.views
               WHERE  Name = 'StudyResultView')
    BEGIN
        EXECUTE ('CREATE VIEW dbo.StudyResultView
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
            WHERE st.PartitionKey = i.PartitionKey
            AND st.StudyKey = i.StudyKey) AS NumberofStudyRelatedInstances,
            st.PartitionKey,
            st.StudyKey
    FROM dbo.Study st');
    END
GO
