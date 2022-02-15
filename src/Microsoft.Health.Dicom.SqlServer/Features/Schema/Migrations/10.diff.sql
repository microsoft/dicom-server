
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

DROP INDEX IF EXISTS IXC_Workitem ON dbo.Workitem
GO
CREATE UNIQUE CLUSTERED INDEX IXC_Workitem ON dbo.Workitem
(
    WorkitemKey
)
GO

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

    SET @watermark = NEXT VALUE FOR dbo.WorkitemWatermarkSequence

    -- The workitem does not exist, insert it.
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

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
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

    IF @@ROWCOUNT = 0
        THROW 50409, 'Workitem update failed.', 1;

    SELECT @@ROWCOUNT

    COMMIT TRANSACTION
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

COMMIT TRANSACTION
