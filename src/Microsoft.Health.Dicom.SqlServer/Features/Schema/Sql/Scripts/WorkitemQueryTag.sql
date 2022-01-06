/*************************************************************
    List of workitem query tags that are supported for UPS-RS queries
**************************************************************/

-- Patient name
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00100010', NULL, 'PN', 0, 1, 1, 0, 1)

-- Patient ID
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00100020', NULL, 'LO', 0, 1, 1, 0, 1)

-- ReferencedRequestSequence.Accesionnumber
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00080050', NULL, 'SH', 0, 1, 1, 0, 1)

-- ReferencedRequestSequence.Requested​Procedure​ID
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00401001', NULL, 'SH', 0, 1, 1, 0, 1)

-- 	Scheduled​Procedure​Step​Start​Date​Time
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00404005', NULL, 'DT', 0, 1, 1, 0, 1)

-- 	ScheduledStationNameCodeSequence.CodeValue
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00404025.00080100', NULL, 'SH', 0, 1, 1, 0, 1)

-- 	ScheduledStationClassCodeSequence.CodeValue
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00404026.00080100', NULL, 'SH', 0, 1, 1, 0, 1)

-- 	Procedure​Step​State
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00741000', NULL, 'CS', 0, 1, 1, 0, 1)

-- 	Scheduled​Station​Geographic​Location​Code​Sequence.CodeValue
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00404027.00080100', NULL, 'SH', 0, 1, 1, 0, 1)

-- 	Transaction​UID
INSERT INTO dbo.ExtendedQueryTag (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus, QueryStatus, ErrorCount, ResourceType)
VALUES (NEXT VALUE FOR TagKeySequence, '00081195', NULL, 'UI', 0, 1, 0, 0, 1)
