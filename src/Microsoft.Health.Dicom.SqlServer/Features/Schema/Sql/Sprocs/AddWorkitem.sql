/*************************************************************
    Stored procedure for adding a workitem.
**************************************************************/
--
-- STORED PROCEDURE
--     AddWorkitem
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
-- RETURN VALUE
--     The WorkitemKey
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddWorkitem
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @newWatermark BIGINT
    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @workitemKey BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', 1;

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    -- The workitem does not exist, insert it.
    SET @workitemKey = NEXT VALUE FOR dbo.WorkitemKeySequence
    INSERT INTO dbo.Workitem
        (WorkitemKey, PartitionKey, WorkitemUid, CreatedDate)
    VALUES
        (@workitemKey, @partitionKey, @workitemUid, @currentDate)

    BEGIN TRY

        EXEC dbo.IIndexInstanceCoreV8
            @partitionKey,
            @workitemKey,
            NULL,
            NULL,
            @newWatermark,
            @stringExtendedQueryTags,
            DEFAULT,
            DEFAULT,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags,
            @workitemResourceType

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @workitemKey

    COMMIT TRANSACTION
END
