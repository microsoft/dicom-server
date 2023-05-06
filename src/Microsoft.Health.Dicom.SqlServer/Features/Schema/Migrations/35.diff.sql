SET XACT_ABORT ON

BEGIN TRANSACTION
/*************************************************************
    DeletedInstance Table
    Add OriginalWatermark
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   (NAME = 'OriginalWatermark')
        AND Object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    ALTER TABLE dbo.DeletedInstance 
    ADD OriginalWatermark BIGINT NULL
END
GO

COMMIT TRANSACTION

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_DeletedInstance'
        AND Object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX IXC_DeletedInstance ON dbo.DeletedInstance
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark
    )
    INCLUDE
    (
        OriginalWatermark
    )
    WITH (DROP_EXISTING=ON, ONLINE=ON)
END
GO
