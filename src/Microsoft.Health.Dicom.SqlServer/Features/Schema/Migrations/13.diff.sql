
/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md

This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION
GO

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

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @currentProcedureStepStateTagValue VARCHAR(64)
    DECLARE @newWatermark BIGINT

    -- Update the workitem watermark
    -- To update the workitem watermark, current watermark MUST match.
    -- This check is to make sure no two parties can update the workitem with an outdated data.
    UPDATE dbo.Workitem
    SET
        Watermark = @proposedWatermark
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

    COMMIT TRANSACTION

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem update failed.', 1;
END
GO

/*************************************************************
    Stored procedure for getting a workitem metadata.
**************************************************************/
--
-- STORED PROCEDURE
--     GetWorkitemMetadata
--
-- DESCRIPTION
--     Gets a UPS-RS workitem metadata.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition Key
--     @workitemInstanceUid
--         * The workitem Key.
--     @procedureStepStateTagPath
--         * The Procedure Step State Tag Path.
-- RETURN VALUE
--     Recordset with the following columns
--          * WorkitemUid
--          * WorkitemKey
--          * PartitionKey
--          * Status
--          * TransactionUid
--          * Watermark
--          * ProcedureStepState Tag Value
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetWorkitemMetadata
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @procedureStepStateTagPath      VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT
	    wi.WorkitemUid,
	    wi.WorkitemKey,
	    wi.PartitionKey,
        wi.[Status],
        wi.TransactionUid,
        wi.Watermark,
        eqt.TagValue AS ProcedureStepState
    FROM 
	    dbo.WorkitemQueryTag wqt
	    INNER JOIN dbo.ExtendedQueryTagString eqt
			ON eqt.ResourceType = 1
			AND eqt.TagKey = wqt.TagKey
			AND wqt.TagPath = @procedureStepStateTagPath
		INNER JOIN dbo.Workitem wi
			ON wi.WorkitemKey = eqt.SopInstanceKey1
			AND wi.PartitionKey = eqt.PartitionKey
    WHERE
        wi.PartitionKey = @partitionKey
        AND wi.WorkitemUid = @workitemUid

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem does not exist', 1;

END
GO

/***************************************************************************
    Stored procedure for getting current and next watermark for a workitem
****************************************************************************/
--
-- STORED PROCEDURE
--     GetCurrentAndNextWorkitemWatermark
--
-- DESCRIPTION
--     Gets the current and next watermark.
--
-- PARAMETERS
--     @workitemKey
--         * The Workitem key.
--
-- RETURN VALUE
--     Recordset with the following columns
--          * Watermark
--          * ProposedWatermark
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetCurrentAndNextWorkitemWatermark
    @workitemKey                       BIGINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    IF NOT EXISTS (SELECT WorkitemKey FROM dbo.Workitem WHERE WorkitemKey = @workitemKey)
        THROW 50409, 'Workitem does not exist', 1;

    DECLARE @proposedWatermark BIGINT
    SET @proposedWatermark = NEXT VALUE FOR dbo.WorkitemWatermarkSequence

    SELECT
        Watermark,
        @proposedWatermark AS ProposedWatermark
    FROM
        dbo.Workitem
    WHERE
        WorkitemKey = @workitemKey

END
GO

IF NOT EXISTS (
    SELECT 1 FROM  dbo.WorkitemQueryTag WHERE TagPath = '0020000D'
)
BEGIN 

    -- Study Instance UID
    INSERT INTO dbo.WorkitemQueryTag (
        TagKey,
        TagPath,
        TagVR
    )
    VALUES (
        NEXT VALUE FOR TagKeySequence,
        '0020000D',
        'UI'
    )

END 
GO

COMMIT TRANSACTION

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_Workitem' AND object_id = OBJECT_ID('dbo.Workitem'))
BEGIN

    CREATE UNIQUE CLUSTERED INDEX IXC_Workitem ON dbo.Workitem
    (
        WorkitemKey
    )

END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IX_Workitem_WorkitemKey_Watermark' AND object_id = OBJECT_ID('dbo.Workitem'))
BEGIN

    CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemKey_Watermark ON dbo.Workitem
    (
        WorkitemKey,
        Watermark
    )
    WITH (DATA_COMPRESSION = PAGE)

END
GO
