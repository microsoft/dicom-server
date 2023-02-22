SET XACT_ABORT ON

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_Status_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid on dbo.Instance
    (
        PartitionKey,
        Status,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    )
    INCLUDE
    (
        Watermark,
        TransferSyntaxUid,
        HasFrameMetadata
    )
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON, DROP_EXISTING=ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_Status_StudyKey_Watermark'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyKey_Watermark on dbo.Instance
    (
        PartitionKey,
        Status,
        StudyKey,
        Watermark
    )
    INCLUDE
    (
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid  
    )
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON, DROP_EXISTING=ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_Status_StudyKey_SeriesKey_Watermark'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyKey_SeriesKey_Watermark on dbo.Instance
    (
        PartitionKey,
        Status,
        StudyKey,
        SeriesKey,
        Watermark
    )
    INCLUDE
    (
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid  
    )
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON, DROP_EXISTING=ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_Watermark'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_Watermark on dbo.Instance
    (
        PartitionKey,
        Watermark
    )
    INCLUDE
    (
        StudyKey,
        SeriesKey,
        StudyInstanceUid
    )
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON, DROP_EXISTING=ON)
END
