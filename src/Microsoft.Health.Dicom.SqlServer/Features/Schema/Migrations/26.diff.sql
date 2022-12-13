SET XACT_ABORT ON

BEGIN TRANSACTION

DROP PROCEDURE IF EXISTS dbo.GetDeletedChangeFeedByWatermarkOrTimeStamp, dbo.GetMaxDeletedChangeFeedWatermark
GO

COMMIT TRANSACTION

