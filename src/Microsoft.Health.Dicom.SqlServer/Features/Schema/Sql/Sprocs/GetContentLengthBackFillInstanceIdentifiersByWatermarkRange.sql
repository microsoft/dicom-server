/**************************************************************/
--
-- STORED PROCEDURE
--     GetContentLengthBackFillInstanceIdentifiersByWatermarkRange
--
-- DESCRIPTION
--     Gets identifiers of instances within the given range of watermarks which need content length backfilled.
--
-- PARAMETERS
--     @startWatermark
--         * The inclusive start watermark.
--     @endWatermark
--         * The inclusive end watermark.
-- RETURN VALUE
--     The instance identifiers.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetContentLengthBackFillInstanceIdentifiersByWatermarkRange
    @startWatermark BIGINT,
    @endWatermark BIGINT
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT I.StudyInstanceUid,
           I.SeriesInstanceUid,
           I.SopInstanceUid,
           I.Watermark,
           P.PartitionName,
           P.PartitionKey
    FROM dbo.Instance I
         INNER JOIN dbo.Partition P
                    ON P.PartitionKey = I.PartitionKey
         INNER JOIN dbo.FileProperty FP
                    ON FP.Watermark = I.Watermark
    WHERE I.Watermark BETWEEN @startWatermark AND @endWatermark
          AND FP.ContentLength = 0
          AND I.Status = 1 -- only backfill instances that are in the 'Created' state
END
