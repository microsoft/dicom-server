/***************************************************************************************/
-- STORED PROCEDURE
--     EndAddInstance
--
-- DESCRIPTION
--     Completes the addition of a DICOM instance.
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark.
--     @maxTagKey
--         * Max ExtendedQueryTag key
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @longExtendedQueryTags
--         * Long extended query tag data
--     @doubleExtendedQueryTags
--         * Double extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.EndAddInstance
    @studyInstanceUid  VARCHAR(64),
    @seriesInstanceUid VARCHAR(64),
    @sopInstanceUid    VARCHAR(64),
    @watermark         BIGINT,
    @maxTagKey         INT = NULL,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        -- This check ensures the client is not potentially missing 1 or more query tags that may need to be indexed.
        -- Note that if @maxTagKey is NULL, < will always return UNKNOWN.
        IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
            THROW 50409, 'Max extended query tag key does not match', 10

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

        UPDATE dbo.Instance
        SET Status = 1, LastStatusUpdatedDate = @currentDate
        WHERE StudyInstanceUid = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid = @sopInstanceUid
            AND Watermark = @watermark

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exist', 1 -- The instance does not exist. Perhaps it was deleted?

        EXEC dbo.IndexInstance
            @watermark,
            @stringExtendedQueryTags,
            @longExtendedQueryTags,
            @doubleExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

        -- Insert to change feed.
        -- Currently this procedure is used only updating the status to created
        -- If that changes an if condition is needed.
        INSERT INTO dbo.ChangeFeed
            (Timestamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
        VALUES
            (@currentDate, 0, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

        -- Update existing instance currentWatermark to latest
        UPDATE dbo.ChangeFeed
        SET CurrentWatermark      = @watermark
        WHERE StudyInstanceUid    = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid    = @sopInstanceUid

    COMMIT TRANSACTION
