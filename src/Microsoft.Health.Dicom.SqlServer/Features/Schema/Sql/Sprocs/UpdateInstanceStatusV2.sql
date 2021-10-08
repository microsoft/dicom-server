/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatusV2
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Updates a DICOM instance status.
--
-- PARAMETERS
--     @partitionName
--         * The client-provided data partition name.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark.
--     @status
--         * The new status to update to.
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV2
    @partitionName      VARCHAR(64),
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @status             TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE PartitionName = @partitionName
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND Watermark = @watermark

    IF @@ROWCOUNT = 0
        -- The instance does not exist. Perhaps it was deleted?
        THROW 50404, 'Instance does not exist', 1;

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Timestamp, Action, PartitionName, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES
        (@currentDate, 0, @partitionName, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE PartitionName = @partitionName
        AND StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid

    COMMIT TRANSACTION
