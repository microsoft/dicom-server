/***************************************************************************************/
-- STORED PROCEDURE
--    UpdateExtendedQueryTagStatus
--
-- DESCRIPTION
--    Delete the specified extended query tag index and its associated metadata
--
-- PARAMETERS
--     @tagKey
--         * The extended query tag key
--     @status
--         * The status to update to. 0 -- Adding, 1 -- Ready, 2 -- Deleting.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateExtendedQueryTagStatus
    @tagKey INT,
    @status TINYINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = @status
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION
END
