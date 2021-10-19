
/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to PersonName
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
            For example, with multiple instances in a series, while indexing a series level tag,
            the Watermark is used to ensure that if there are different values between instances,
            the value on the instance with the highest watermark wins.
    Note: The primary key is designed on the assumption that tags only occur once in an instance.
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagPersonName (
    TagKey                  INT                  NOT NULL,              --FK
    TagValue                NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI NOT NULL,
    StudyKey                BIGINT               NOT NULL,              --FK
    SeriesKey               BIGINT               NULL,                  --FK
    InstanceKey             BIGINT               NULL,                  --FK
    Watermark               BIGINT               NOT NULL,
    WatermarkAndTagKey      AS CONCAT(TagKey, '.', Watermark),          --PK
    TagValueWords           AS REPLACE(REPLACE(TagValue, '^', ' '), '=', ' ') PERSISTED
) WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName ON dbo.ExtendedQueryTagPersonName
(
    TagKey,
    TagValue,
    StudyKey,
    SeriesKey,
    InstanceKey
)

-- Used in IIndexInstanceCore
CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_TagKey_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagPersonName
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
CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_StudyKey_SeriesKey_InstanceKey on dbo.ExtendedQueryTagPersonName
(
    StudyKey,
    SeriesKey,
    InstanceKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE NONCLUSTERED INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey ON dbo.ExtendedQueryTagPersonName
(
    WatermarkAndTagKey
)
WITH (DATA_COMPRESSION = PAGE)
