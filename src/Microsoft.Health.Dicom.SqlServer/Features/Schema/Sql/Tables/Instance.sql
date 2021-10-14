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
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Instance on dbo.Instance
(
    SeriesKey,
    InstanceKey
)

--Filter indexes
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

CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_StudyInstanceUid_Status on dbo.Instance
(
    PartitionKey,
    StudyInstanceUid,
    Status
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_StudyInstanceUid_SeriesInstanceUid_Status on dbo.Instance
(
    PartitionKey,
    StudyInstanceUid,
    SeriesInstanceUid,
    Status
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_SopInstanceUid_Status on dbo.Instance
(
    PartitionKey,
    SopInstanceUid,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_Watermark_Status on dbo.Instance
(
    Watermark,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
WITH (DATA_COMPRESSION = PAGE)

--Cross apply indexes
CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_SeriesKey_Status on dbo.Instance
(
    PartitionKey,
    SeriesKey,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_StudyKey_Status on dbo.Instance
(
    PartitionKey,
    StudyKey,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)
