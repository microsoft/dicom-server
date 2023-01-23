/*************************************************************
    Workitem Table
**************************************************************/
CREATE TABLE dbo.Workitem (
    WorkitemKey                 BIGINT                            NOT NULL,             --PK
    PartitionKey                INT                               NOT NULL DEFAULT 1,   --FK
    WorkitemUid                 VARCHAR(64)                       NOT NULL,
    TransactionUid              VARCHAR(64)                       NULL,
    Status                      TINYINT                           NOT NULL,
    --audit columns
    CreatedDate                 DATETIME2(7)                      NOT NULL,
    LastStatusUpdatedDate       DATETIME2(7)                      NOT NULL,
    Watermark                   BIGINT                            DEFAULT 0 NOT NULL,
) WITH (DATA_COMPRESSION = PAGE)


-- used in GetWorkitemMetadata, DeleteWorkitem
CREATE UNIQUE CLUSTERED INDEX IXC_Workitem ON dbo.Workitem
(
    PartitionKey,
    WorkitemKey
)


-- used in GetWorkitemMetadata, AddWorkitem
CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_PartitionKey_WorkitemUid ON dbo.Workitem
(
    PartitionKey,
    WorkitemUid
)
INCLUDE
(
    Watermark,
    WorkitemKey,
    Status,
    TransactionUid
)
WITH (DATA_COMPRESSION = PAGE)

-- cross partition
CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemKey_Watermark ON dbo.Workitem
(
    WorkitemKey,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)
