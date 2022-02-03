/*************************************************************
    Stored procedure for getting a workitem detail.
**************************************************************/
--
-- STORED PROCEDURE
--     GetWorkitemDetail
--
-- DESCRIPTION
--     Gets a UPS-RS workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
-- RETURN VALUE
--     The WorkitemKey
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetWorkitemDetail
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT
	    wi.WorkitemUid,
	    wi.WorkitemKey,
	    wi.PartitionKey,
	    eqt.TagValue AS ProcedureStepState
    FROM 
	    dbo.ExtendedQueryTagString eqt
	    INNER JOIN dbo.WorkitemQueryTag wqt
            ON wqt.TagKey = eqt.TagKey AND wqt.TagPath = '00741000' -- TagPath for Procedure Step State
	    INNER JOIN dbo.Workitem wi
            ON wi.WorkitemKey = eqt.SopInstanceKey1 AND wi.PartitionKey = eqt.PartitionKey
    WHERE
        eqt.ResourceType = 1
	    AND wi.PartitionKey = @partitionKey
	    AND wi.WorkitemUid = @workitemUid

END
