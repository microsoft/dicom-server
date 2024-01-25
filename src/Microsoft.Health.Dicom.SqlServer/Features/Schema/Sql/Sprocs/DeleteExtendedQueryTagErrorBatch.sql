/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTagErrorBatch
--
-- DESCRIPTION
--    Delete a batch of the extended query tag errors for a specific tag
--
-- PARAMETERS
--     @tagPath
--         * The key of the extended query tag
--     @batchSize
--         * the size of each deletion batch
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagErrorBatch
    @tagKey INT,
    @batchSize INT = 1000
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    -- TODO: is this still necessary if there is no while loop here?
    EXEC dbo.ISleepIfBusy

    BEGIN TRANSACTION

        DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagError
        WHERE TagKey = @tagKey

    COMMIT TRANSACTION
END
