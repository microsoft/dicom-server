/*************************************************************
    Stored procedure for adding a workitem.
**************************************************************/
--
-- STORED PROCEDURE
--     AddWorkitemV10
--
-- DESCRIPTION
--     Adds a UPS-RS workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
--     @initialStatus
--         * New status of the workitem status, Either 0(None) or 1(ReadWrite)
-- RETURN VALUE
--     The WorkitemKey and Watermark
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddWorkitemV10
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
    @initialStatus                  TINYINT
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @workitemKey BIGINT
    DECLARE @watermark BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', 1;

    -- The workitem does not exist, insert it.
    SET @watermark = NEXT VALUE FOR dbo.WorkitemWatermarkSequence
    SET @workitemKey = NEXT VALUE FOR dbo.WorkitemKeySequence
    INSERT INTO dbo.Workitem
        (WorkitemKey, PartitionKey, WorkitemUid, Status, Watermark, CreatedDate, LastStatusUpdatedDate)
    VALUES
        (@workitemKey, @partitionKey, @workitemUid, @initialStatus, @watermark, @currentDate, @currentDate)

    BEGIN TRY

        EXEC dbo.IIndexWorkitemInstanceCore
            @partitionKey,
            @workitemKey,
            @stringExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT
        @workitemKey AS WorkitemKey,
        @watermark AS Watermark

    COMMIT TRANSACTION
END
