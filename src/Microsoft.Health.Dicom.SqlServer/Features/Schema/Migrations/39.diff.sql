SET XACT_ABORT ON

BEGIN TRANSACTION
GO

CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestByTimeV39
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) c.Sequence,
                     c.Timestamp,
                     c.Action,
                     p.PartitionName,
                     c.StudyInstanceUid,
                     c.SeriesInstanceUid,
                     c.SopInstanceUid,
                     c.OriginalWatermark,
                     c.CurrentWatermark,
                     f.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
             LEFT OUTER JOIN
             dbo.Instance AS i
             ON i.StudyInstanceUid = c.StudyInstanceUid
                AND i.SeriesInstanceUid = c.SeriesInstanceUid
                AND i.SopInstanceUid = c.SopInstanceUid
             LEFT OUTER JOIN
             dbo.FileProperty AS f
             ON f.InstanceKey = i.InstanceKey
    ORDER BY c.Timestamp DESC, c.Sequence DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV39
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   TOP (1) c.Sequence,
                     c.Timestamp,
                     c.Action,
                     p.PartitionName,
                     c.StudyInstanceUid,
                     c.SeriesInstanceUid,
                     c.SopInstanceUid,
                     c.OriginalWatermark,
                     c.CurrentWatermark,
                     f.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
             LEFT OUTER JOIN
             dbo.Instance AS i
             ON i.StudyInstanceUid = c.StudyInstanceUid
                AND i.SeriesInstanceUid = c.SeriesInstanceUid
                AND i.SopInstanceUid = c.SopInstanceUid
             LEFT OUTER JOIN
             dbo.FileProperty AS f
             ON f.InstanceKey = i.InstanceKey
    ORDER BY c.Sequence DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV39
@limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   c.Sequence,
             c.Timestamp,
             c.Action,
             p.PartitionName,
             c.StudyInstanceUid,
             c.SeriesInstanceUid,
             c.SopInstanceUid,
             c.OriginalWatermark,
             c.CurrentWatermark,
             f.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
             LEFT OUTER JOIN
             dbo.Instance AS i
             ON i.StudyInstanceUid = c.StudyInstanceUid
                AND i.SeriesInstanceUid = c.SeriesInstanceUid
                AND i.SopInstanceUid = c.SopInstanceUid
             LEFT OUTER JOIN
             dbo.FileProperty AS f
             ON f.InstanceKey = i.InstanceKey
    WHERE    c.Sequence BETWEEN @offset + 1 AND @offset + @limit
    ORDER BY c.Sequence;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetChangeFeedByTimeV39
@startTime DATETIMEOFFSET (7), @endTime DATETIMEOFFSET (7), @limit INT, @offset BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT   c.Sequence,
             c.Timestamp,
             c.Action,
             p.PartitionName,
             c.StudyInstanceUid,
             c.SeriesInstanceUid,
             c.SopInstanceUid,
             c.OriginalWatermark,
             c.CurrentWatermark,
             f.FilePath
    FROM     dbo.ChangeFeed AS c WITH (HOLDLOCK)
             INNER JOIN
             dbo.Partition AS p
             ON p.PartitionKey = c.PartitionKey
             LEFT OUTER JOIN
             dbo.Instance AS i
             ON i.StudyInstanceUid = c.StudyInstanceUid
                AND i.SeriesInstanceUid = c.SeriesInstanceUid
                AND i.SopInstanceUid = c.SopInstanceUid
             LEFT OUTER JOIN
             dbo.FileProperty AS f
             ON f.InstanceKey = i.InstanceKey
    WHERE    c.Timestamp >= @startTime
             AND c.Timestamp < @endTime
    ORDER BY c.Timestamp, c.Sequence
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
END
GO

COMMIT TRANSACTION