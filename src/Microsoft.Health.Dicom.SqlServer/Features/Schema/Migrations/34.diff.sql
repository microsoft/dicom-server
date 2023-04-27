SET XACT_ABORT ON

IF NOT EXISTS
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_ChangeFeed_Timestamp'
        AND Object_id = OBJECT_ID('dbo.ChangeFeed')
)
BEGIN

    CREATE NONCLUSTERED INDEX IX_ChangeFeed_Timestamp ON dbo.ChangeFeed
    (
        Timestamp
    ) WITH (DATA_COMPRESSION = PAGE)

END
GO

BEGIN TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestTimestamp
--
-- FIRST SCHEMA VERSION
--     34
--
-- DESCRIPTION
--     Gets the dicom change with the latest timestamp
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestTimestamp
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            PartitionName,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed c
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    ORDER BY Timestamp DESC
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedPage
--
-- FIRST SCHEMA VERSION
--     34
--
-- DESCRIPTION
--     Gets a subset of dicom changes within a given time range
--
-- PARAMETERS
--     @startTime
--         * Inclusive timestamp start
--     @endTime
--         * Exclusive timestamp end
--     @offet
--         * Rows to skip
--     @limit
--         * Max rows to return
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedPage (
    @startTime DATETIMEOFFSET(7),
    @endTime   DATETIMEOFFSET(7),
    @offset    BIGINT,
    @limit     INT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT
        Sequence,
        Timestamp,
        Action,
        PartitionName,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        OriginalWatermark,
        CurrentWatermark
    FROM dbo.ChangeFeed c
    INNER JOIN dbo.Partition p
    ON p.PartitionKey = c.PartitionKey
    WHERE c.Timestamp >= @startTime AND c.Timestamp < @endTime
    ORDER BY Timestamp ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

COMMIT TRANSACTION
GO
