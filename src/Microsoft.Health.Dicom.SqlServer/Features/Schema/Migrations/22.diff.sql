/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/

SET XACT_ABORT ON

/****************************************************************************************
Stored Procedures
******************************************************************************************/
BEGIN TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    UpdateIndexWorkitemInstanceCore
--
-- DESCRIPTION
--    Updates workitem query tag values.
--    
-- PARAMETERS
--     @workitemKey
--         * Refers to WorkItemKey
--     @partitionKey
--         * Refers to Partition Key
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateIndexWorkitemInstanceCore
    @workitemKey                                                                 BIGINT,
    @partitionKey                                                                INT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN

    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @newWatermark BIGINT

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN

        UPDATE ets
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        FROM dbo.ExtendedQueryTagString AS ets
        INNER JOIN @stringExtendedQueryTags AS input
            ON ets.TagKey = input.TagKey
        WHERE
            SopInstanceKey1 = @workitemKey
            AND ResourceType = @workitemResourceType
            AND PartitionKey = @partitionKey
            AND ets.TagValue <> input.TagValue

    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN

        UPDATE etdt
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        FROM dbo.ExtendedQueryTagDateTime AS etdt
        INNER JOIN @dateTimeExtendedQueryTags AS input
            ON etdt.TagKey = input.TagKey
        WHERE
            SopInstanceKey1 = @workitemKey
            AND ResourceType = @workitemResourceType
            AND PartitionKey = @partitionKey
            AND etdt.TagValue <> input.TagValue

    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN

        UPDATE etpn
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        FROM dbo.ExtendedQueryTagPersonName AS etpn
        INNER JOIN @personNameExtendedQueryTags AS input
            ON etpn.TagKey = input.TagKey
        WHERE
            SopInstanceKey1 = @workitemKey
            AND ResourceType = @workitemResourceType
            AND PartitionKey = @partitionKey
            AND etpn.TagValue <> input.TagValue

    END
END
GO


/*************************************************************
    Stored procedure for updating a workitem transaction.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitemTransaction
--
-- DESCRIPTION
--     Update a UPS-RS workitem.
--
-- PARAMETERS
--     @workitemKey
--         * The workitem key.
--     @partitionKey
--         * Refers to Partition Key
--     @watermark
--         * The existing workitem watermark.
--     @proposedWatermark
--         * The proposed watermark for the workitem.
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemTransaction
    @workitemKey                    BIGINT,
    @partitionKey                   INT,
    @watermark                      BIGINT,
    @proposedWatermark              BIGINT,
    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @newWatermark AS BIGINT;
    DECLARE @currentDate AS DATETIME2(7) = SYSUTCDATETIME();

    -- To update the workitem watermark, current watermark MUST match.
    -- This check is to make sure no two parties can update the workitem with the outdated data.
    UPDATE dbo.Workitem
    SET
        Watermark = @proposedWatermark
    WHERE
        WorkitemKey = @workitemKey
        AND Watermark = @watermark

    IF @@ROWCOUNT = 0
        THROW 50499, 'Workitem update failed', 1;

    BEGIN TRY

        EXEC dbo.UpdateIndexWorkitemInstanceCore
            @workitemKey,
            @partitionKey,
            @stringExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

    END TRY

    BEGIN CATCH

        THROW;

    END CATCH

    COMMIT TRANSACTION;
END
GO


COMMIT TRANSACTION
GO
