/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance V6
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance.
--
-- PARAMETERS
--     @watermark
--         * The Dicom instance watermark.
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
--
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV6
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT    ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

        -- Fetch the instance with the given watermark and lock the row to prevent deletion
        -- Note that re-indexing only affect instances that have been completed added and as
        -- such cannot have their status changed during re-indexing.
        DECLARE @partitionKey BIGINT
        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT
        DECLARE @status TINYINT

        SELECT
            @partitionKey = PartitionKey,
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @status = Status
        FROM dbo.Instance WITH (UPDLOCK)
        WHERE Watermark = @watermark

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exists', 1
        IF @status <> 1 -- Created
            THROW 50409, 'Instance has not yet been stored succssfully', 1

        -- Optionally lock the study and/or series tables if the one or more tag levels
        -- are more coarse grain than instance, as we need to ensure we can safely update.
        DECLARE @maxTagLevel TINYINT

        SELECT @maxTagLevel = MAX(TagLevel)
        FROM
        (
            SELECT TagLevel FROM @stringExtendedQueryTags
            UNION ALL
            SELECT TagLevel FROM @longExtendedQueryTags
            UNION ALL
            SELECT TagLevel FROM @doubleExtendedQueryTags
            UNION ALL
            SELECT TagLevel FROM @dateTimeExtendedQueryTags
            UNION ALL
            SELECT TagLevel FROM @personNameExtendedQueryTags
        ) AS AllEntries

        IF @maxTagLevel > 1
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Study WITH (UPDLOCK) WHERE PartitionKey = @partitionKey AND StudyKey = @studyKey)
                THROW 50404, 'Study does not exists', 1
        END

        IF @maxTagLevel > 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Series WITH (UPDLOCK) WHERE PartitionKey = @partitionKey AND StudyKey = @studyKey AND SeriesKey = @seriesKey)
                THROW 50404, 'Series does not exists', 1
        END

        -- Insert Extended Query Tags
        BEGIN TRY

            EXEC dbo.IIndexInstanceCoreV9
                @partitionKey,
                @studyKey,
                @seriesKey,
                @instanceKey,
                @watermark,
                @stringExtendedQueryTags,
                @longExtendedQueryTags,
                @doubleExtendedQueryTags,
                @dateTimeExtendedQueryTags,
                @personNameExtendedQueryTags

        END TRY
        BEGIN CATCH

            THROW

        END CATCH

    COMMIT TRANSACTION
END
