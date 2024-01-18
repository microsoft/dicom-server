SET XACT_ABORT ON
    
BEGIN TRANSACTION
GO
/*************************************************************
    Stored procedures for updating content length of a file property
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateFilePropertiesContentLength
--
-- DESCRIPTION
--     Update content length for a list of file properties matching the watermark.
--
-- PARAMETERS
--     @filePropertiesToUpdate
--         * A table type containing the file properties with content length that needs to be updated
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateFilePropertiesContentLength
@filePropertiesToUpdate dbo.FilePropertyTableType_2 READONLY
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION
    UPDATE FP
    SET    ContentLength = FPTU.ContentLength
    FROM   dbo.FileProperty FP
           INNER JOIN @filePropertiesToUpdate FPTU
           ON FP.Watermark = FPTU.Watermark
    COMMIT TRANSACTION
END
GO


/***************************************************************************************/
-- STORED PROCEDURE
--     GetContentLengthBackFillInstanceBatches
--
-- DESCRIPTION
--     Divides up the instances into a configurable number of batches and only targets instances whose
--     file properties have a content length of 0
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @batchCount
--         * The desired number of batches. Actual number may be smaller.
--
-- RETURN VALUE
--     The batches as defined by their inclusive minimum and maximum values.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetContentLengthBackFillInstanceBatches
@batchSize INT, @batchCount INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT   MIN(Watermark) AS MinWatermark,
             MAX(Watermark) AS MaxWatermark
    FROM     (
        SELECT TOP (@batchSize * @batchCount) I.Watermark,
               (ROW_NUMBER() OVER (ORDER BY I.Watermark DESC) - 1) / @batchSize AS Batch
        FROM dbo.Instance AS I
        INNER JOIN dbo.FileProperty AS FP
        ON FP.Watermark = I.Watermark
        WHERE  FP.ContentLength = 0) AS I
    GROUP BY Batch
    ORDER BY Batch ASC
END
GO


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
    @startWatermark BIGINT, @endWatermark BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT I.StudyInstanceUid,
           I.SeriesInstanceUid,
           I.SopInstanceUid,
           I.Watermark,
           P.PartitionName,
           P.PartitionKey
    FROM dbo.Instance AS I
    INNER JOIN dbo.Partition AS P
    ON P.PartitionKey = I.PartitionKey
    INNER JOIN dbo.FileProperty AS FP
    ON FP.Watermark = I.Watermark
    WHERE I.Watermark BETWEEN @startWatermark AND @endWatermark
    AND FP.ContentLength = 0
    AND I.Status = 1 -- only backfill instances that are in the 'Created' state
END
GO

COMMIT TRANSACTION


IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_FileProperty_InstanceKey_Watermark_ContentLength'
        AND Object_id = OBJECT_ID('dbo.FileProperty')
)
BEGIN
    CREATE NONCLUSTERED INDEX IXC_FileProperty_InstanceKey_Watermark_ContentLength ON dbo.FileProperty
    (
    InstanceKey,
    Watermark,
    ContentLength
    ) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON)
END
GO