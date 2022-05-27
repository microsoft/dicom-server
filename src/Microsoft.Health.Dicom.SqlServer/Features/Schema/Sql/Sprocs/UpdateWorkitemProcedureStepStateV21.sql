/*************************************************************
 Stored procedure for Updating a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitemProcedureStepStateV21
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
--     @transactionUid
--          * The Transaction UID for the Workitem transaction
--
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
            DECLARE @transactionUid VARCHAR(64) = NULL

	        EXECUTE [dbo].[UpdateWorkitemProcedureStepStateV21] 
	           @workitemKey
	          ,@procedureStepStateTagPath
	          ,@procedureStepState
              ,@watermark
	          ,@proposedWatermark
              ,@transactionUid

        END
        GO

*/
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemProcedureStepStateV21
    @workitemKey                    BIGINT,
    @procedureStepStateTagPath      VARCHAR(64),
    @procedureStepState             VARCHAR(64),
    @watermark                      BIGINT,
    @proposedWatermark              BIGINT,
    @transactionUid                 VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @newWatermark BIGINT
    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @currentProcedureStepStateTagValue VARCHAR(64)

    -- Update the workitem watermark
    -- To update the workitem watermark, current watermark MUST match.
    -- This check is to make sure no two parties can update the workitem with the outdated data.
    UPDATE dbo.Workitem
    SET
        Watermark = @proposedWatermark,
        TransactionUid = @transactionUid
    WHERE
        WorkitemKey = @workitemKey
        AND Watermark = @watermark

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem update failed.', 1;

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence;
    -- Update the Tag Value
    WITH TagKeyCTE AS (
	    SELECT
		    wqt.TagKey,
		    wqt.TagPath,
		    eqts.TagValue AS OldTagValue,
            eqts.ResourceType,
		    wi.PartitionKey,
		    wi.WorkitemKey,
		    eqts.Watermark AS ExtendedQueryTagWatermark
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
		    AND cte.ExtendedQueryTagWatermark = targetTbl.Watermark
    WHERE
        cte.TagPath = @procedureStepStateTagPath;

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem procedure step state update failed.', 1;

    COMMIT TRANSACTION

END
GO
