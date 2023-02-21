/*************************************************************
    Instance Table
    Dicom instances with unique Partition Name, and Study, Series and Instance Uid
**************************************************************/
CREATE TABLE dbo.Instance (
    InstanceKey             BIGINT                     NOT NULL,            --PK
    SeriesKey               BIGINT                     NOT NULL,            --FK
    -- StudyKey needed to join directly from Study table to find a instance
    StudyKey                BIGINT                     NOT NULL,            --FK
    --instance keys used in WADO
    StudyInstanceUid        VARCHAR(64)                NOT NULL,
    SeriesInstanceUid       VARCHAR(64)                NOT NULL,
    SopInstanceUid          VARCHAR(64)                NOT NULL,
    --data consistency columns
    Watermark               BIGINT                     NOT NULL,
    Status                  TINYINT                    NOT NULL,
    LastStatusUpdatedDate   DATETIME2(7)               NOT NULL,
    --audit columns
    CreatedDate             DATETIME2(7)               NOT NULL,
    PartitionKey            INT                        NOT NULL DEFAULT 1,  --FK
    --instance metadata
    TransferSyntaxUid       VARCHAR(64)                NULL,
    HasFrameMetadata        BIT                        NOT NULL DEFAULT 0
) WITH (DATA_COMPRESSION = PAGE)

-- Primary index, also used in views
CREATE UNIQUE CLUSTERED INDEX IXC_Instance on dbo.Instance
(
    PartitionKey,
    StudyKey,
    SeriesKey,
    InstanceKey
)

-- Used in AddInstance, DeleteInstance
CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid on dbo.Instance
(
    PartitionKey,
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
INCLUDE
(
    Status,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in WADO
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
WITH (DATA_COMPRESSION = PAGE)

-- Used in GetInstancesByWatermarkRange
CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_Watermark_Status on dbo.Instance
(
    Watermark,
    Status
)
INCLUDE
(
    PartitionKey,
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
WITH (DATA_COMPRESSION = PAGE)

-- QIDO filtering
CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_SopInstanceUid ON dbo.Instance
(
    PartitionKey,
    SopInstanceUid
)
INCLUDE
(
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

-- QIDO Cross apply indexes
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
WITH (DATA_COMPRESSION = PAGE)

-- QIDO Cross apply indexes
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
WITH (DATA_COMPRESSION = PAGE)

-- Used in Study/Series views
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
WITH (DATA_COMPRESSION = PAGE)
