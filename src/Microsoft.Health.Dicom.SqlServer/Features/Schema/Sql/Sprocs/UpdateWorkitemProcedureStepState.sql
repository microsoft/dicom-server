/*************************************************************
 Stored procedure for Updating a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitemProcedureStepState
--
-- DESCRIPTION
--     Updates a UPS-RS workitem procedure step state.
--
-- PARAMETERS
--     @workitemKey
--         * The Workitem Key.
--     @procedureStepStateTagPath
--          * The Procedure Step State Tag Path
--     @procedureStepState
--          * The New Procedure Step State Value
--     @watermark
--          * The Workitem Watermark
--     @proposedWatermark
--          * The Proposed Watermark for the Workitem
--
-- RETURN VALUE
--     The Number of records affected in ExtendedQueryTagString table
--
-- EXAMPLE
--
/*

        BEGIN
	        DECLARE @workitemKey BIGINT = 1
	        DECLARE @procedureStepStateTagPath varchar(64) = '00741000'
	        DECLARE @procedureStepState varchar(64) = 'SCHEDULED'
	        DECLARE @watermark BIGINT = 101
            DECLARE @proposedWatermark BIGINT = 201

	        EXECUTE [dbo].[UpdateWorkitemProcedureStepState] 
	           @workitemKey
	          ,@procedureStepStateTagPath
	          ,@procedureStepState
              ,@watermark
	          ,@proposedWatermark
        END
        GO

*/
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemProcedureStepState
    @workitemKey                    BIGINT,
    @procedureStepStateTagPath      VARCHAR(64),
    @procedureStepState             VARCHAR(64),
    @watermark                      BIGINT,
    @proposedWatermark              BIGINT
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    IF NOT EXISTS (SELECT WorkitemUid FROM Workitem WHERE WorkitemKey = @workitemKey)
        THROW 50409, 'Workitem does not exist', 1;

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    -- Update the workitem watermark
    UPDATE dbo.Workitem
    SET
        Watermark = @proposedWatermark
    WHERE
        WorkitemKey = @workitemKey
        AND Watermark = @watermark

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem update failed.', 1;

    DECLARE @currentProcedureStepStateTagValue VARCHAR(64)
    DECLARE @newWatermark BIGINT
    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    BEGIN TRY

        -- Update the Tag Value
        WITH TagKeyCTE AS (
	        SELECT
		        wqt.TagKey,
		        wqt.TagPath,
		        eqts.TagValue AS OldTagValue,
                eqts.ResourceType,
		        wi.PartitionKey,
		        wi.WorkitemKey,
		        wi.WorkitemUid,
		        wi.TransactionUid,
		        eqts.Watermark
	        FROM 
		        dbo.WorkitemQueryTag wqt
		        INNER JOIN dbo.ExtendedQueryTagString eqts ON 
			        eqts.TagKey = wqt.TagKey 
			        AND eqts.ResourceType = 1 -- Workitem Resource Type
		        INNER JOIN dbo.Workitem wi ON
			        wi.PartitionKey = eqts.PartitionKey
			        AND wi.WorkitemKey = eqts.SopInstanceKey1
	        WHERE
		        wi.WorkitemKey = @workitemKey
        )
        UPDATE targetTbl
        SET
            targetTbl.TagValue = @procedureStepState,
            targetTbl.Watermark = @newWatermark
        FROM
	        dbo.ExtendedQueryTagString targetTbl
	        INNER JOIN TagKeyCTE cte ON
		        targetTbl.ResourceType = cte.ResourceType
		        AND cte.PartitionKey = targetTbl.PartitionKey
		        AND cte.WorkitemKey = targetTbl.SopInstanceKey1
		        AND cte.TagKey = targetTbl.TagKey
		        AND cte.OldTagValue = targetTbl.TagValue
		        AND cte.Watermark = targetTbl.Watermark
        WHERE
            cte.TagPath = @procedureStepStateTagPath

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @@ROWCOUNT

    COMMIT TRANSACTION
END
