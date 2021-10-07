/*************************************************************
    Partition Table
    Table containing data partitions for light-weight multitenancy.
**************************************************************/
CREATE TABLE dbo.Partition (
    PartitionKey                INT             NOT NULL, --PK  System-generated sequence
    PartitionName               VARCHAR(64)     NOT NULL, --    Client-generated unique name. Length allows GUID or UID.
    -- audit columns
    CreatedDate                 DATETIME2(7)    NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Partition ON dbo.Partition
(
    PartitionKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_Partition_PartitionName ON dbo.Study
(
    PartitionName
) WITH (DATA_COMPRESSION = PAGE)

-- Add default partition values
INSERT INTO dbo.Partition
    (PartitionKey, PartitionName, CreatedDate)
VALUES
    (1, 'Microsoft.Default', SYSUTCDATETIME())
