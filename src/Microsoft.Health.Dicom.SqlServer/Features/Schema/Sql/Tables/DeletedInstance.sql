/*************************************************************
    DeletedInstance Table
    Table containing deleted instances that will be removed after the specified date
**************************************************************/
CREATE TABLE dbo.DeletedInstance
(
    StudyInstanceUid    VARCHAR(64)       NOT NULL,
    SeriesInstanceUid   VARCHAR(64)       NOT NULL,
    SopInstanceUid      VARCHAR(64)       NOT NULL,
    Watermark           BIGINT            NOT NULL,
    DeletedDateTime     DATETIMEOFFSET(0) NOT NULL,
    RetryCount          INT               NOT NULL,
    CleanupAfter        DATETIMEOFFSET(0) NOT NULL,
    PartitionKey        INT               NOT NULL DEFAULT 1    --FK
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_DeletedInstance ON dbo.DeletedInstance
(
    PartitionKey,
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark
)

-- Used in RetrieveDeletedInstance
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
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

