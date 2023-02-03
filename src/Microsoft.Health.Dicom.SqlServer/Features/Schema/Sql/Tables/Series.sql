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

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_SeriesInstanceUid ON dbo.Series
(
    PartitionKey,
    SeriesInstanceUid
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_Modality ON dbo.Series
(
    PartitionKey,
    Modality
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_PerformedProcedureStepStartDate ON dbo.Series
(
    PartitionKey,
    PerformedProcedureStepStartDate
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Series_PartitionKey_ManufacturerModelName ON dbo.Series
(
    PartitionKey,
    ManufacturerModelName
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)
