
/*************************************************************
    Extended Query Tag Data Table for VR Types mapping to PersonName
    Note: Watermark is primarily used while re-indexing to determine which TagValue is latest.
            For example, with multiple instances in a series, while indexing a series level tag,
            the Watermark is used to ensure that if there are different values between instances,
            the value on the instance with the highest watermark wins.
    Note: The primary key is designed on the assumption that tags only occur once in an instance.
    
            This table includes tags used by both Image SOP instances and UPS SOP instances, which are distinguished by resource type column.
            - ResourceType = 0 means Image SOP instance, ResourceType = 1 means UPS SOP instance.
            - SopInstanceKey1 refers to either StudyKey or WorkItemKey.
            - SopInstanceKey2 refers to SeriesKey if ResourceType is Image else NULL.
            - SopInstanceKey3 refers to InstanceKey if ResourceType is Image else NULL.
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagPersonName (
    TagKey                  INT                  NOT NULL,              --FK
    TagValue                NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI NOT NULL,
    SopInstanceKey1         BIGINT               NOT NULL,              --FK
    SopInstanceKey2         BIGINT               NULL,                  --FK
    SopInstanceKey3         BIGINT               NULL,                  --FK
    Watermark               BIGINT               NOT NULL,
    WatermarkAndTagKey      AS CONCAT(TagKey, '.', Watermark),          --PK
    TagValueWords           AS REPLACE(REPLACE(TagValue, '^', ' '), '=', ' ') PERSISTED,
    PartitionKey            INT                  NOT NULL DEFAULT 1,    --FK
    ResourceType            TINYINT              NOT NULL DEFAULT 0 
) WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagPersonName ON dbo.ExtendedQueryTagPersonName
(
    PartitionKey,
    ResourceType,
    TagKey,
    TagValue,
    SopInstanceKey1,
    SopInstanceKey2,
    SopInstanceKey3
)

-- Used in IIndexInstanceCore
CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_TagKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagPersonName
(
    PartitionKey,
    ResourceType,
    TagKey,
    SopInstanceKey1,
    SopInstanceKey2,
    SopInstanceKey3
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in DeleteInstance
CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagPersonName_PartitionKey_ResourceType_SopInstanceKey1_SopInstanceKey2_SopInstanceKey3 on dbo.ExtendedQueryTagPersonName
(
    PartitionKey,
    ResourceType,
    SopInstanceKey1,
    SopInstanceKey2,
    SopInstanceKey3
)
WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE NONCLUSTERED INDEX IXC_ExtendedQueryTagPersonName_WatermarkAndTagKey ON dbo.ExtendedQueryTagPersonName
(
    WatermarkAndTagKey
)
WITH (DATA_COMPRESSION = PAGE)
