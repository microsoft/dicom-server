/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to String
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
            For example, with multiple instances in a series, while indexing a series level tag,
            the Watermark is used to ensure that if there are different values between instances,
            the value on the instance with the highest watermark wins.
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagString (
    TagKey                  INT                  NOT NULL,              --PK
    TagValue                NVARCHAR(64)         NOT NULL,
    StudyKey                BIGINT               NOT NULL,              --FK
    SeriesKey               BIGINT               NULL,                  --FK
    InstanceKey             BIGINT               NULL,                  --FK
    Watermark               BIGINT               NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagString ON dbo.ExtendedQueryTagString
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

-- Used in IIndexInstanceCore
CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagString_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagString
(
    TagKey,
    StudyKey,
    SeriesKey,
    InstanceKey
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in DeleteInstance
CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagString_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagString
(
    StudyKey,
    SeriesKey,
    InstanceKey
)
WITH (DATA_COMPRESSION = PAGE)
