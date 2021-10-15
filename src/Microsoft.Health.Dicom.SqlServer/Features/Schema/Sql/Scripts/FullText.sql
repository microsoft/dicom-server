/*************************************************************
Full text catalog and index creation outside transaction
**************************************************************/
IF NOT EXISTS (
    SELECT *
    FROM sys.fulltext_catalogs
    WHERE name = 'Dicom_Catalog')
BEGIN
    CREATE FULLTEXT CATALOG Dicom_Catalog WITH ACCENT_SENSITIVITY = OFF AS DEFAULT
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.fulltext_indexes
    where object_id = object_id('dbo.Study'))
BEGIN
    CREATE FULLTEXT INDEX ON Study(PatientNameWords, ReferringPhysicianNameWords LANGUAGE 1033)
    KEY INDEX IX_Study_StudyKey
    WITH STOPLIST = OFF;
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.fulltext_indexes
    where object_id = object_id('dbo.ExtendedQueryTagPersonName'))
BEGIN
    CREATE FULLTEXT INDEX ON ExtendedQueryTagPersonName(TagValueWords LANGUAGE 1033)
    KEY INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey
    WITH STOPLIST = OFF;
END