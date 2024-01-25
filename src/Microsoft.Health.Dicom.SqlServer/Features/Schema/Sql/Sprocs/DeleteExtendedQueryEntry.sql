/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTagEntry
--
-- DESCRIPTION
--    Delete the specified extended query tag entry
--
-- PARAMETERS
--     @tagKey
--         * The extended query tag key
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagEntry
    @tagKey INT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    -- Delete tag
    DELETE FROM dbo.ExtendedQueryTag
    WHERE TagKey = @tagKey
END
