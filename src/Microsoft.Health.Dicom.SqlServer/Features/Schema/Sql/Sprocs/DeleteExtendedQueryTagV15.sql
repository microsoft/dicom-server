/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTagV15
--
-- DESCRIPTION
--    Delete the specified extended query tag index and its associated metadata
--
-- PARAMETERS
--     @tagPath
--         * The extended query tag path
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
--     @batchSize
--         * the size of each deletion batch
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagV15
    @tagPath VARCHAR(64),
    @dataType TINYINT,
    @batchSize INT = 1000
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @tagKey INT
        DECLARE @imageResourceType TINYINT = 0

        SELECT @tagKey = TagKey
        FROM dbo.ExtendedQueryTag WITH(XLOCK)
        WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION

    BEGIN TRANSACTION

        -- Delete index data
        DECLARE @deleted INT = 1

        WHILE @deleted > 0
        BEGIN
            IF @dataType = 0
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagString WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE IF @dataType = 1
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagLong WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE IF @dataType = 2
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagDouble WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE IF @dataType = 3
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagDateTime WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagPersonName WHERE TagKey = @tagKey AND ResourceType = @imageResourceType

            SET @deleted = @@ROWCOUNT
        END

        -- Delete errors
        SET @deleted = 1

        WHILE @deleted > 0
        BEGIN
            DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagError
            WHERE TagKey = @tagKey

            SET @deleted = @@ROWCOUNT
        END

        -- Delete tag
        DELETE FROM dbo.ExtendedQueryTag
        WHERE TagKey = @tagKey

    COMMIT TRANSACTION
END
