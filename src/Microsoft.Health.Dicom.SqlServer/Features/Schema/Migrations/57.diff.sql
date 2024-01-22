SET XACT_ABORT ON
    
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