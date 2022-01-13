/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteWorkitem
--
-- DESCRIPTION
--    Delete specific workitem and its query tag values
--
-- PARAMETERS
--     @partitionKey
--         * The Partition Key
--     @workitemUid
--         * The workitem UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteWorkitem
    @partitionKey INT,
    @workitemUid  VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @workitemResourceType TINYINT = 1
        DECLARE @workitemKey BIGINT

        SELECT @workitemKey = WorkitemKey
        FROM dbo.Workitem
        WHERE PartitionKey = @partitionKey
            AND WorkitemUid = @workitemUid

        -- Check existence
        IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exists', 1;

        DELETE FROM dbo.ExtendedQueryTagString
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagLong
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagDouble
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagDateTime
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.ExtendedQueryTagPersonName
        WHERE SopInstanceKey1 = @workitemKey
            AND PartitionKey = @partitionKey
            AND ResourceType = @workitemResourceType

        DELETE FROM dbo.Workitem
        WHERE WorkItemKey = @workitemKey
            AND PartitionKey = @partitionKey

    COMMIT TRANSACTION
END
