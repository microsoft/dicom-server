/***************************************************************************************/
-- STORED PROCEDURE
--    UpdateExtendedQueryTagStatusToDelete
--
-- DESCRIPTION
--    Updates the extended query tag status to 'Deleting'
--
-- PARAMETERS
--     @tagKey
--         * The extended query tag key
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateExtendedQueryTagStatusToDelete
    @tagKey INT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @tagStatus TINYINT

        SELECT @tagStatus = TagStatus
        FROM dbo.ExtendedQueryTag WITH(XLOCK)
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1

        -- check if status is Ready or Adding
        IF @tagStatus = 2
            THROW 50412, 'extended query tag is not in Ready or Adding status', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION
END
