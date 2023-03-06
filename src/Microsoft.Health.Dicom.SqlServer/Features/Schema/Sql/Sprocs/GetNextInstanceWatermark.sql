/***************************************************************************
    Stored procedure for getting current and next watermark for a workitem
****************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetNextInstanceWatermark
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @proposedWatermark BIGINT
    SET @proposedWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    SELECT
        @proposedWatermark AS ProposedWatermark
    FROM
        dbo.Instance
END
