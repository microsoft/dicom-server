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
