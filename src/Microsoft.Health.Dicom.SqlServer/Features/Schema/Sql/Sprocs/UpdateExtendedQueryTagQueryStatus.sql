/***************************************************************************************/
-- STORED PROCEDURE
--     UpdateExtendedQueryTagQueryStatus
--
-- DESCRIPTION
--    Update QueryStatus of extended query tag
--
-- PARAMETERS
--     @tagPath
--         * The extended query tag path
--     @queryStatus
--         * The query  status
--
-- RETURN VALUE
--     The modified extended query tag.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateExtendedQueryTagQueryStatus
    @tagPath VARCHAR(64),
    @queryStatus TINYINT
AS
BEGIN
    SET NOCOUNT     ON

    UPDATE XQT
    SET QueryStatus = @queryStatus
    OUTPUT
        INSERTED.TagKey,
        INSERTED.TagPath,
        INSERTED.TagVR,
        INSERTED.TagPrivateCreator,
        INSERTED.TagLevel,
        INSERTED.TagStatus,
        INSERTED.QueryStatus,
        INSERTED.ErrorCount,
        XQTO.OperationId
    FROM dbo.ExtendedQueryTag AS XQT
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    WHERE TagPath = @tagPath
END