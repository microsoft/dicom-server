/*************************************************************
    Stored procedure for updating a workitem status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitemStatus
--
-- DESCRIPTION
--     Updates a workitem status.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemKey
--         * The workitem key.
--     @status
--         * Initial status of the workitem status, Either 0(Creating) or 1(Created)
-- RETURN VALUE
--     The WorkitemKey
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemStatus
    @partitionKey                   INT,
    @workitemKey                    BIGINT,
    @status                         TINYINT
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Workitem
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE PartitionKey = @partitionKey
        AND WorkitemKey = @workitemKey

    -- The workitem instance does not exist. Perhaps it was deleted?
    IF @@ROWCOUNT = 0
        THROW 50404, 'Workitem instance does not exist', 1

    COMMIT TRANSACTION
END
