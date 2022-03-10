/*************************************************************************
    List of workitem query tags that are supported for UPS-RS queries
*************************************************************************/

-- Patient name
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00100010', 'PN')

-- Patient ID
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00100020', 'LO')

-- ReferencedRequestSequence.Accesionnumber
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00080050', 'SQ')

-- ReferencedRequestSequence.Requested​Procedure​ID
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '0040A370.00401001', 'SQ')

-- Scheduled​Procedure​Step​Start​Date​Time
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00404005', 'DT')

-- ScheduledStationNameCodeSequence.CodeValue
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00404025.00080100', 'SQ')

-- ScheduledStationClassCodeSequence.CodeValue
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00404026.00080100', 'SQ')

-- Procedure​Step​State
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00741000', 'CS')

-- Scheduled​Station​Geographic​Location​Code​Sequence.CodeValue
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '00404027.00080100', 'SQ')

-- Study Instance UID
INSERT INTO dbo.WorkitemQueryTag (TagKey, TagPath, TagVR)
VALUES (NEXT VALUE FOR TagKeySequence, '0020000D', 'UI')
