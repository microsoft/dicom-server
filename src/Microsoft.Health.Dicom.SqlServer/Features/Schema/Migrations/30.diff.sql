SET XACT_ABORT ON

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Series_PartitionKey_SeriesInstanceUid'
        AND Object_id = OBJECT_ID('dbo.Series')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_SeriesInstanceUid ON dbo.Series
    (
        PartitionKey,
        SeriesInstanceUid
    )
    INCLUDE
    (
        StudyKey
    )
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON, DROP_EXISTING=ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_SopInstanceUid'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_SopInstanceUid ON dbo.Instance
    (
        PartitionKey,
        SopInstanceUid
    )
    INCLUDE
    (
        SeriesKey
    )
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON, DROP_EXISTING=ON)
END
