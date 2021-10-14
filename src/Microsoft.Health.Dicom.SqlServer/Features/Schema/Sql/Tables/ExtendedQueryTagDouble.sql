
/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to Double
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
            For example, with multiple instances in a series, while indexing a series level tag,
            the Watermark is used to ensure that if there are different values between instances,
            the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagDouble (
    TagKey                  INT                  NOT NULL,              --PK
    TagValue                FLOAT(53)            NOT NULL,
    StudyKey                BIGINT               NOT NULL,              --FK
    SeriesKey               BIGINT               NULL,                  --FK
    InstanceKey             BIGINT               NULL,                  --FK
    Watermark               BIGINT               NOT NULL,
    PartitionKey            INT                  NOT NULL DEFAULT 1     --FK
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDouble ON dbo.ExtendedQueryTagDouble
(
    TagKey,
    TagValue,
    PartitionKey,
    StudyKey,
    SeriesKey,
    InstanceKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagDouble_TagKey_PartitionKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagDouble
(
    TagKey,
    PartitionKey,
    StudyKey,
    SeriesKey,
    InstanceKey
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)
