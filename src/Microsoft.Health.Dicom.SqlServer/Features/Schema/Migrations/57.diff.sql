SET XACT_ABORT ON

BEGIN TRANSACTION
GO
CREATE OR ALTER PROCEDURE dbo.GetIndexedFileMetrics
    AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
SELECT TotalIndexedFileCount=COUNT_BIG(*),
       TotalIndexedBytes=SUM(ContentLength)
FROM   dbo.FileProperty;
END
GO

COMMIT TRANSACTION
    
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_FileProperty_ContentLength'
        AND Object_id = OBJECT_ID('dbo.FileProperty')
)
BEGIN
    CREATE NONCLUSTERED INDEX IXC_FileProperty_ContentLength ON dbo.FileProperty
    (
    ContentLength
    ) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON)
END
GO