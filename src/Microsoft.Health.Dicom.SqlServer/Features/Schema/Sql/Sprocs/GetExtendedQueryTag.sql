
/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTag
--
-- DESCRIPTION
--     Gets all extended query tags or given extended query tag by tag path
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag to retrieve.
-- RETURN VALUE
--     The desired extended query tag, if found.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTag
    @tagPath  VARCHAR(64) = NULL -- Support NULL for backwards compatibility
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @imageResourceType TINYINT = 0

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM dbo.ExtendedQueryTag AS XQT
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    WHERE TagPath = ISNULL(@tagPath, TagPath)
    AND ResourceType = @imageResourceType
END
