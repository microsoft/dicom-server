/*************************************************************
    Series Table
    Table containing normalized standard Series tags
**************************************************************/

CREATE TABLE dbo.Series (
    SeriesKey                           BIGINT                     NOT NULL,            --PK
    StudyKey                            BIGINT                     NOT NULL,            --FK
    SeriesInstanceUid                   VARCHAR(64)                NOT NULL,
    Modality                            NVARCHAR(16)               NULL,
    PerformedProcedureStepStartDate     DATE                       NULL,
    ManufacturerModelName               NVARCHAR(64)               NULL,
    PartitionKey                        INT                        NOT NULL DEFAULT 1   --FK
) WITH (DATA_COMPRESSION = PAGE)

-- Ordering studies by partition, study, and series key for partition-specific retrieval, Also used in STOW
CREATE UNIQUE CLUSTERED INDEX IXC_Series ON dbo.Series
(
    PartitionKey,
    StudyKey,
    SeriesKey
)

-- used in STOW and Delete
CREATE UNIQUE NONCLUSTERED INDEX IX_Series_PartitionKey_StudyKey_SeriesInstanceUid ON dbo.Series
(
    PartitionKey,
    StudyKey,
    SeriesInstanceUid
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO when querying at the study level; UIDs will not be used cross partition
CREATE UNIQUE NONCLUSTERED INDEX IX_Series_PartitionKey_SeriesInstanceUid ON dbo.Series
(
    PartitionKey,
    SeriesInstanceUid
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Series_Modality_PartitionKey ON dbo.Series
(
    Modality,
    PartitionKey
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Series_PerformedProcedureStepStartDate_PartitionKey ON dbo.Series
(
    PerformedProcedureStepStartDate,
    PartitionKey
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Series_ManufacturerModelName_PartitionKey ON dbo.Series
(
    ManufacturerModelName,
    PartitionKey
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)
