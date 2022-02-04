
/*************************************************************
 Stored procedure for Updating a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitemProcedureStepState
--
-- DESCRIPTION
--     Adds a UPS-RS workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
--     @procedureStepStateTagPath
--          * The Procedure Step State Tag Path
--     @procedureStepState
--          * The New Procedure Step State Value
--     @status
--         * The Workitem status, Either 1(ReadWrite) or 2(Read)
--
-- RETURN VALUE
--     The WorkitemKey
--
-- EXAMPLE
--
/*

        BEGIN
	        DECLARE @partitionKey int = 1
	        DECLARE @workitemUid varchar(64) = '1.2.840.10008.1.2.184951'
	        DECLARE @procedureStepStateTagPath varchar(64) = '00741000'
	        DECLARE @procedureStepState varchar(64) = 'SCHEDULED'
	        DECLARE @status tinyint = 1

	        EXECUTE [dbo].[UpdateWorkitemProcedureStepState] 
	           @partitionKey
	          ,@workitemUid
	          ,@procedureStepStateTagPath
	          ,@procedureStepState
	          ,@status
        END
        GO

*/
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitemProcedureStepState
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @procedureStepStateTagPath      VARCHAR(64),
    @procedureStepState             VARCHAR(64),
    @status                         TINYINT
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @workitemKey BIGINT
    DECLARE @newWatermark BIGINT
    DECLARE @currentProcedureStepStateTagValue VARCHAR(64)
    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    SELECT
        @workitemKey = WorkitemKey
    FROM
        dbo.Workitem
    WHERE
        PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem does not exist', 1;

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
		        wi.PartitionKey = @partitionKey
		        AND wi.WorkitemKey = @workitemKey
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

        -- Update the Workitem status
        EXEC dbo.UpdateWorkitemStatus @partitionKey, @workitemKey, @status

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @workitemKey

    COMMIT TRANSACTION
END
