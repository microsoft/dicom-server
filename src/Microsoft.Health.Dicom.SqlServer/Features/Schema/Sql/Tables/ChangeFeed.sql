/*************************************************************
    Changes Table
    Stores Add/Delete immutable actions
    Only CurrentWatermark is updated to reflect the current state.
    Current Instance State
    CurrentWatermark = null,               Current State = Deleted
    CurrentWatermark = OriginalWatermark,  Current State = Created
    CurrentWatermark <> OriginalWatermark, Current State = Replaced
**************************************************************/
CREATE TABLE dbo.ChangeFeed (
    Sequence                BIGINT IDENTITY(1,1) NOT NULL,
    Timestamp               DATETIMEOFFSET(7)    NOT NULL DEFAULT SYSDATETIMEOFFSET(),  -- Automatically populate
    Action                  TINYINT              NOT NULL,
    StudyInstanceUid        VARCHAR(64)          NOT NULL,
    SeriesInstanceUid       VARCHAR(64)          NOT NULL,
    SopInstanceUid          VARCHAR(64)          NOT NULL,
    OriginalWatermark       BIGINT               NOT NULL,
    CurrentWatermark        BIGINT               NULL,
    PartitionKey            INT                  NOT NULL DEFAULT 1,                    -- FK
    FilePath                NVARCHAR(4000)       NULL,                                  -- Copied from FileProperty to avoid hash match on joins later
) WITH (DATA_COMPRESSION = PAGE)

-- Change feed is cross partition
-- Note: While the Change Feed is unique on Sequence, Timestamp is included to alter the physical sort order
-- in support of the V2 APIs and beyond that filter the change feed based on time
CREATE UNIQUE CLUSTERED INDEX IXC_ChangeFeed ON dbo.ChangeFeed
(
    Timestamp,
    Sequence
)

-- Used to update all change feed entries for a particular instance (e.g. DeleteInstance)
CREATE NONCLUSTERED INDEX IX_ChangeFeed_PartitionKey_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed
(
    PartitionKey,
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
) WITH (DATA_COMPRESSION = PAGE)

-- For use with the V1 APIs that use Sequence
CREATE NONCLUSTERED INDEX IX_ChangeFeed_Sequence ON dbo.ChangeFeed
(
    Sequence
) WITH (DATA_COMPRESSION = PAGE)
