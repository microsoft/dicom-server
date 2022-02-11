/*********************************************************
    Stored procedure for getting watermark detail
**********************************************************/
--
-- STORED PROCEDURE
--     GetWorkitemWatermark
--
-- DESCRIPTION
--     Gets a watermark and proposed watermark for the workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
--     @workitemUid
--         * The Workitem instance UID.
--
-- RETURN VALUE
--     Recordset with the following columns
--          * WorkitemKey
--          * Watermark
--          * ProposedWartermark
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetWorkitemWatermark
    @partitionKey                       INT,
    @workitemUid                        VARCHAR(64)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @proposedWatermark BIGINT
    SET @proposedWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    SELECT
        WorkitemKey,
        Watermark,
        @proposedWatermark AS ProposedWatermark
    FROM
        Workitem
    WHERE
        PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

END
