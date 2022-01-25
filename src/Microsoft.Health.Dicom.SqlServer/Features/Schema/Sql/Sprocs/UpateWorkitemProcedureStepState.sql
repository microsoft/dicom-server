/*************************************************************
    Stored procedure to update a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpateWorkitemProcedureStepState
--
-- DESCRIPTION
--     Update a UPS-RS Workitem Procedure Step State.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
--     @procedure​Step​StateTagPath
--         * Procedure Step State Tag Path
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpateWorkitemProcedureStepState
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),
    @procedureStepStateTagPath      VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @workitemKey BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exists', 1;

    -- Step:0 - Get Tag Key from WorkitemQueryTag using @procedure​Step​StateTagPath

    -- Step: 1 - Update ExtendedQueryTagString Set

    COMMIT TRANSACTION
END
