SET XACT_ABORT ON

BEGIN TRANSACTION
GO

/*************************************************************
    sproc updates
**************************************************************/

CREATE OR ALTER PROCEDURE dbo.RetrieveDeletedInstanceV42
@count INT, @maxRetries INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@count) p.PartitionName,
                        d.PartitionKey,
                        d.StudyInstanceUid,
                        d.SeriesInstanceUid,
                        d.SopInstanceUid,
                        d.Watermark,
                        d.OriginalWatermark
    FROM   dbo.DeletedInstance AS d WITH (UPDLOCK, READPAST)
           INNER JOIN
           dbo.Partition AS p
           ON p.PartitionKey = d.PartitionKey
    WHERE  RetryCount <= @maxRetries
           AND CleanupAfter < SYSUTCDATETIME();
END
GO

COMMIT TRANSACTION