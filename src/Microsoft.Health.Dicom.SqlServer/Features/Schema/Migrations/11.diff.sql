/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/

SET XACT_ABORT ON

BEGIN TRANSACTION

IF NOT EXISTS
(
    SELECT * FROM sys.sequences
    WHERE Name = 'WorkitemWatermarkSequence'
)
BEGIN
    CREATE SEQUENCE dbo.WorkitemWatermarkSequence
        AS BIGINT
        START WITH 1
        INCREMENT BY 1
        MINVALUE 1
        NO CYCLE
        CACHE 10000
END
GO

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'Watermark'
        AND Object_id = OBJECT_ID('dbo.Workitem')
)
BEGIN
    ALTER TABLE dbo.Workitem
        ADD Watermark BIGINT DEFAULT 0 NOT NULL
END
GO

/*************************************************************
    Stored procedure for adding a workitem.
**************************************************************/
--
-- STORED PROCEDURE
--     AddWorkitemV11
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
--         * New status of the workitem, Either 0(Creating) or 1(Created)
-- RETURN VALUE
--     The WorkitemKey
--     The Watermark
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddWorkitemV11
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

    DECLARE @watermark BIGINT
    DECLARE @workitemKey BIGINT
    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT <> 0
        THROW 50409, 'Workitem already exists', 1;

    -- The workitem does not exist, insert it.
    SET @workitemKey = NEXT VALUE FOR dbo.WorkitemKeySequence
    SET @watermark = NEXT VALUE FOR dbo.WorkitemWatermarkSequence
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

    COMMIT TRANSACTION

    SELECT
        @workitemKey,
        @watermark

END
GO

COMMIT TRANSACTION

DROP INDEX IF EXISTS IX_Workitem_WorkitemUid_PartitionKey ON dbo.Workitem
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemUid_PartitionKey ON dbo.Workitem
(
    WorkitemUid,
    PartitionKey
)
INCLUDE
(
    Watermark,
    WorkitemKey,
    Status,
    TransactionUid
)
WITH (DATA_COMPRESSION = PAGE)
GO

DROP INDEX IF EXISTS IX_Workitem_WorkitemKey_Watermark ON dbo.Workitem
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemKey_Watermark ON dbo.Workitem
(
    WorkitemKey,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)
GO
