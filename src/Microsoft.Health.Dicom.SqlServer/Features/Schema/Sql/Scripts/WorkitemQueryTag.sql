/*************************************************************
    List of workitem query tags that are supported for UPS-RS queries
**************************************************************/

-- Patient name
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00100010', 'PN', 1)

-- Patient ID
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00100020', 'LO', 1)

-- ReferencedRequestSequence.Accesionnumber
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00080050', 'SH', 1)

-- ReferencedRequestSequence.Requested​Procedure​ID
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00401001', 'SH', 1)

-- 	Scheduled​Procedure​Step​Start​Date​Time
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00404005', 'DT', 1)

-- 	ScheduledStationNameCodeSequence.CodeValue
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00404025.00080100', 'SH', 1)

-- 	ScheduledStationClassCodeSequence.CodeValue
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00404026.00080100', 'SH', 1)

-- 	Procedure​Step​State
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00741000', 'CS', 1)

-- 	Scheduled​Station​Geographic​Location​Code​Sequence.CodeValue
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00404027.00080100', 'SH', 1)

-- 	Transaction​UID
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR, QueryStatus)
VALUES (NEXT VALUE FOR TagKeySequence, '00081195', 'UI', 0)
