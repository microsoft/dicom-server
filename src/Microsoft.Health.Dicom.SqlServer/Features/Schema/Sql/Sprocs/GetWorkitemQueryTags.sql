/***************************************************************************************/
-- STORED PROCEDURE
--     GetWorkitemQueryTags
--
-- DESCRIPTION
--     Gets indexed workitem query tags
--
-- RETURN VALUE
--     The set of workitem query tags.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetWorkitemQueryTags
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT TagKey,
           TagPath,
           TagVR
    FROM dbo.WorkItemQueryTag
END
