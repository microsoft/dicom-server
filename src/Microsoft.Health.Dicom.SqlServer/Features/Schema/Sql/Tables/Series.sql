/*************************************************************
    Series Table
    Table containing normalized standard Series tags
**************************************************************/

CREATE TABLE dbo.Series (
    SeriesKey                           BIGINT                     NOT NULL,            --PK
    PartitionKey                        INT                        NOT NULL DEFAULT 1,  --FK
    StudyKey                            BIGINT                     NOT NULL,            --FK
    SeriesInstanceUid                   VARCHAR(64)                NOT NULL,
    Modality                            NVARCHAR(16)               NULL,
    PerformedProcedureStepStartDate     DATE                       NULL,
    ManufacturerModelName               NVARCHAR(64)               NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Series ON dbo.Series
(
    PartitionKey,
    StudyKey,
    SeriesKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_Series_SeriesKey ON dbo.Series
(
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE NONCLUSTERED INDEX IX_Series_StudyKey_SeriesInstanceUid ON dbo.Series
(
    StudyKey,
    SeriesInstanceUid
) WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Series_Modality ON dbo.Series
(
    Modality
) WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Series_PerformedProcedureStepStartDate ON dbo.Series
(
    PerformedProcedureStepStartDate
) WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Series_ManufacturerModelName ON dbo.Series
(
    ManufacturerModelName
) WITH (DATA_COMPRESSION = PAGE)
