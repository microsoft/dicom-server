
/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to DateTime
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
            For example, with multiple instances in a series, while indexing a series level tag,
            the Watermark is used to ensure that if there are different values between instances,
            the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagDateTime (
    TagKey                  INT                  NOT NULL,              --PK
    TagValue                DATETIME2(7)         NOT NULL,
    StudyKey                BIGINT               NOT NULL,              --FK
    SeriesKey               BIGINT               NULL,                  --FK
    InstanceKey             BIGINT               NULL,                  --FK
    Watermark               BIGINT               NOT NULL,
    TagValueUtc             DATETIME2(7)         NULL,
    PartitionKey            INT                  NOT NULL DEFAULT 1     --FK
) WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO, Adding PartitionKey to the end to enable cross-partition queries
CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey,
    PartitionKey
)

-- Used in IIndexInstanceCore, Adding PartitionKey to the end to enable cross-partition queries
CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDateTime_TagKey_StudyKey_SeriesKey_InstanceKey_PartitionKey on dbo.ExtendedQueryTagDateTime
(
    TagKey,
    StudyKey,
    SeriesKey,
    InstanceKey,
    PartitionKey
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)
