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

-- Used in AddInstance/STOW
CREATE UNIQUE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyInstanceUid ON dbo.Study
(
    PartitionKey,
    StudyInstanceUid
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_PatientId ON dbo.Study
(
    PartitionKey,
    PatientId
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_PatientName ON dbo.Study
(
    PartitionKey,
    PatientName   
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_ReferringPhysicianName ON dbo.Study
(
   PartitionKey,
   ReferringPhysicianName
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyDate ON dbo.Study
(
    PartitionKey,
    StudyDate
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_StudyDescription ON dbo.Study
(
    PartitionKey,
    StudyDescription
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_AccessionNumber ON dbo.Study
(
    PartitionKey,
    AccessionNumber   
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in QIDO
CREATE NONCLUSTERED INDEX IX_Study_PartitionKey_PatientBirthDate ON dbo.Study
(
    PartitionKey,
    PatientBirthDate  
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)
