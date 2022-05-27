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

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence;

    BEGIN TRY

        EXEC dbo.UpdateIndexWorkitemInstanceCore
            @workitemKey,
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
