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
    WHERE   NAME = 'IX_DeletedInstance_RetryCount_CleanupAfter'
        AND Object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeletedInstance_RetryCount_CleanupAfter ON dbo.DeletedInstance
    (
        RetryCount,
        CleanupAfter
    )
    INCLUDE
    (
        PartitionKey,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        OriginalWatermark
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING=ON, ONLINE=ON)
END
GO
