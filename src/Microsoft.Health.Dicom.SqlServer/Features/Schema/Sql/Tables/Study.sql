/*************************************************************
    Study Table
    Table containing normalized standard Study tags
**************************************************************/
CREATE TABLE dbo.Study (
    StudyKey                    BIGINT                            NOT NULL,             --PK
    StudyInstanceUid            VARCHAR(64)                       NOT NULL,
    PatientId                   NVARCHAR(64)                      NOT NULL,
    PatientName                 NVARCHAR(200)                     COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    ReferringPhysicianName      NVARCHAR(200)                     COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    StudyDate                   DATE                              NULL,
    StudyDescription            NVARCHAR(64)                      NULL,
    AccessionNumber             NVARCHAR(16)                      NULL,
    PatientNameWords            AS REPLACE(REPLACE(PatientName, '^', ' '), '=', ' ') PERSISTED,
    ReferringPhysicianNameWords AS REPLACE(REPLACE(ReferringPhysicianName, '^', ' '), '=', ' ') PERSISTED,
    PatientBirthDate            DATE                              NULL,
    PartitionKey                INT                               NOT NULL DEFAULT 1    --FK
) WITH (DATA_COMPRESSION = PAGE)

-- Ordering studies by partition and then by study key for partition-specific retrieval
CREATE UNIQUE CLUSTERED INDEX IXC_Study ON dbo.Study
(
    PartitionKey,
    StudyKey
)

-- Used as the unique index for full-text index - must be a unique, non-nullable, single-column index
CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyKey ON dbo.Study
(
    StudyKey
) WITH (DATA_COMPRESSION = PAGE)

-- Used in AddInstance; we place PartitionKey second because we assume conflicting StudyInstanceUid will be rare
CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyInstanceUid_PartitionKey ON dbo.Study
(
    StudyInstanceUid,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_PatientId_PartitionKey ON dbo.Study
(
    PatientId,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_PatientName_PartitionKey ON dbo.Study
(
    PatientName,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_ReferringPhysicianName_PartitionKey ON dbo.Study
(
    ReferringPhysicianName,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_StudyDate_PartitionKey ON dbo.Study
(
    StudyDate,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_StudyDescription_PartitionKey ON dbo.Study
(
    StudyDescription,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_AccessionNumber_PartitionKey ON dbo.Study
(
    AccessionNumber,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO; putting PartitionKey second allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Study_PatientBirthDate_PartitionKey ON dbo.Study
(
    PatientBirthDate,
    PartitionKey
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)
