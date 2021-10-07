
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
    PartitionKey            INT                  NOT NULL DEFAULT 1,    --FK
    StudyKey                BIGINT               NOT NULL,              --FK
    SeriesKey               BIGINT               NULL,                  --FK
    InstanceKey             BIGINT               NULL,                  --FK
    Watermark               BIGINT               NOT NULL,
    TagValueUtc             DATETIME2(7)         NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagDateTime ON dbo.ExtendedQueryTagDateTime
(
    TagKey,
    TagValue,
    PartitionKey,
    StudyKey,
    SeriesKey,
    InstanceKey
)
