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
    Timestamp               DATETIMEOFFSET(7)    NOT NULL,
    Action                  TINYINT              NOT NULL,
    StudyInstanceUid        VARCHAR(64)          NOT NULL,
    SeriesInstanceUid       VARCHAR(64)          NOT NULL,
    SopInstanceUid          VARCHAR(64)          NOT NULL,
    OriginalWatermark       BIGINT               NOT NULL,
    CurrentWatermark        BIGINT               NULL,
    PartitionName           VARCHAR(64)          NOT NULL DEFAULT 'Microsoft.Default'
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_ChangeFeed ON dbo.ChangeFeed
(
    Sequence
)

CREATE NONCLUSTERED INDEX IX_ChangeFeed_PartitionName_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed
(
    PartitionName,
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
