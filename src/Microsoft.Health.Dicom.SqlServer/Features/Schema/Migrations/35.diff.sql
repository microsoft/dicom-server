SET XACT_ABORT ON

BEGIN TRANSACTION

    DROP INDEX IF EXISTS IXC_ChangeFeed ON dbo.ChangeFeed

    CREATE UNIQUE CLUSTERED INDEX IXC_ChangeFeed ON dbo.ChangeFeed
    (
        Timestamp,
        Sequence
    )

COMMIT TRANSACTION
GO

BEGIN TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatestV35
--
-- FIRST SCHEMA VERSION
--     35
--
-- DESCRIPTION
--     Gets the latest dicom change by timestamp
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedLatestV35
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
    ORDER BY Timestamp DESC, Sequence DESC
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedV35
--
-- FIRST SCHEMA VERSION
--     35
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
CREATE OR ALTER PROCEDURE dbo.GetChangeFeedV35 (
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
    ORDER BY Timestamp, Sequence
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

/***************************************************************************************/
--STORED PROCEDURE
--     GetExtendedQueryTagsV35
--
-- FIRST SCHEMA VERSION
--     35
--
-- DESCRIPTION
--     Gets a possibly paginated set of query tags as indicated by the parameters
--
-- PARAMETERS
--     @limit
--         * The maximum number of results to retrieve.
--     @offset
--         * The offset from which to retrieve paginated results.
--
-- RETURN VALUE
--     The set of query tags.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagsV35
    @limit  INT,
    @offset BIGINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT XQT.TagKey,
           TagPath,
           TagVR,
           TagPrivateCreator,
           TagLevel,
           TagStatus,
           QueryStatus,
           ErrorCount,
           OperationId
    FROM dbo.ExtendedQueryTag AS XQT
    LEFT OUTER JOIN dbo.ExtendedQueryTagOperation AS XQTO ON XQT.TagKey = XQTO.TagKey
    ORDER BY XQT.TagKey ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagErrorsV35
--
-- FIRST SCHEMA VERSION
--     35
--
-- DESCRIPTION
--     Gets the extended query tag errors by tag path.
--
-- PARAMETERS
--     @tagPath
--         * The TagPath for the extended query tag for which we retrieve error(s).
--     @limit
--         * The maximum number of results to retrieve.
--     @offset
--         * The offset from which to retrieve paginated results.
--
-- RETURN VALUE
--     The tag error fields and the corresponding instance UIDs.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagErrorsV35
    @tagPath VARCHAR(64),
    @limit   INT,
    @offset  BIGINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @tagKey INT
    SELECT @tagKey = TagKey
    FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)
    WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

    -- Check existence
    IF (@@ROWCOUNT = 0)
        THROW 50404, 'extended query tag not found', 1

    SELECT
        TagKey,
        ErrorCode,
        CreatedTime,
        PartitionName,
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid
    FROM dbo.ExtendedQueryTagError AS XQTE
    INNER JOIN dbo.Instance AS I
    ON XQTE.Watermark = I.Watermark
    INNER JOIN dbo.Partition P
    ON P.PartitionKey = I.PartitionKey
    WHERE XQTE.TagKey = @tagKey
    ORDER BY CreatedTime ASC, XQTE.Watermark ASC, TagKey ASC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY
END
GO

COMMIT TRANSACTION
GO
