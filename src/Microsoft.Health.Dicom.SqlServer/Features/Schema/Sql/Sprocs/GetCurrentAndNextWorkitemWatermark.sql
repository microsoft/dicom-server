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
--     @partitionKey
--         * The partition key.
--     @workitemUid
--         * The Workitem instance UID.
--
-- RETURN VALUE
--     Recordset with the following columns
--          * Watermark
--          * ProposedWatermark
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetCurrentAndNextWorkitemWatermark
    @partitionKey                       INT,
    @workitemUid                        VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @workitemKey BIGINT

    SELECT
        @workitemKey = WorkitemKey
    FROM
        dbo.Workitem
    WHERE
        PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid        

    IF @workitemKey IS NULL
        THROW 50409, 'Workitem does not exist', 1;

    DECLARE @proposedWatermark BIGINT
    SET @proposedWatermark = NEXT VALUE FOR dbo.WorkitemWatermarkSequence

    SELECT
        Watermark,
        @proposedWatermark AS ProposedWatermark
    FROM
        Workitem
    WHERE
        WorkitemKey = @workitemKey

END
