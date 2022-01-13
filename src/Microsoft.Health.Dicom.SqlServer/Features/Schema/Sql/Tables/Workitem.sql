/*************************************************************
    Workitem Table
**************************************************************/
CREATE TABLE dbo.Workitem (
    WorkitemKey                 BIGINT                            NOT NULL,             --PK
    PartitionKey                INT                               NOT NULL DEFAULT 1,   --FK
    WorkitemUid                 VARCHAR(64)                       NOT NULL,
    TransactionUid               VARCHAR(64)                      NULL,
    --audit columns
    CreatedDate                 DATETIME2(7)                      NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

-- Ordering workitems by partition and then by WorkitemKey for partition-specific retrieval
CREATE UNIQUE CLUSTERED INDEX IXC_Workitem ON dbo.Workitem
(
    PartitionKey,
    WorkitemKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_Workitem_WorkitemUid_PartitionKey ON dbo.Workitem
(
    WorkitemUid,
    PartitionKey
)
INCLUDE
(
    WorkitemKey,
    TransactionUid
)
WITH (DATA_COMPRESSION = PAGE)
