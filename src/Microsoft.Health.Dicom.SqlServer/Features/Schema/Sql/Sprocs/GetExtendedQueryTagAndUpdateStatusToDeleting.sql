/***************************************************************************************/
-- STORED PROCEDURE
--    GetExtendedQueryTagAndUpdateStatusToDeleting
--
-- DESCRIPTION
--    Delete the specified extended query tag index and its associated metadata
--
-- PARAMETERS
--     @tagPath
--         * The extended query tag path
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagAndUpdateStatusToDeleting
    @tagPath VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @tagStatus TINYINT
        DECLARE @tagKey INT

        SELECT @tagKey = TagKey, @tagStatus = TagStatus
        FROM dbo.ExtendedQueryTag WITH(XLOCK)
        WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1

        -- TODO: Do we still need this check, is it ok if its currently deleting
        -- check if status is Ready or Adding
        IF @tagStatus = 2
            THROW 50412, 'extended query tag is not in Ready or Adding status', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

        -- return tag key
        SELECT @tagKey

    COMMIT TRANSACTION
END
