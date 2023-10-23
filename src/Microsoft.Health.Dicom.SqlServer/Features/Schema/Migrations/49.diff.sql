SET XACT_ABORT ON

BEGIN TRANSACTION
GO
      
/*************************************************************
    SPROC Updates
**************************************************************/

      
/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstanceV42
--
-- FIRST SCHEMA VERSION
--     6
--
-- CHANGES
-- Retrieve additional fields for file properties
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV42
    @count          INT,
    @maxRetries     INT
    AS
BEGIN
    SET NOCOUNT ON

    SELECT  TOP (@count) p.PartitionName, d.PartitionKey, d.StudyInstanceUid, d.SeriesInstanceUid, d.SopInstanceUid, d.Watermark, d.OriginalWatermark, d.FilePath, d.ETag
    FROM    dbo.DeletedInstance as d WITH (UPDLOCK, READPAST)
    INNER JOIN dbo.Partition as p
    ON p.PartitionKey = d.PartitionKey
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
END
GO

COMMIT TRANSACTION