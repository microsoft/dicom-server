/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTagIndexBatch
--
-- DESCRIPTION
--    Delete the specified extended query tag index
--
-- PARAMETERS
--     @tagKey
--         * The extended query tag key
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
--     @batchSize
--         * the size of each deletion batch
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagIndexBatch
    @tagKey INT,
    @dataType TINYINT,
    @batchSize INT = 1000
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @imageResourceType TINYINT = 0

    -- is this needed? Will this affect sproc row count?
    EXEC dbo.ISleepIfBusy

    BEGIN TRANSACTION

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

    COMMIT TRANSACTION
END
