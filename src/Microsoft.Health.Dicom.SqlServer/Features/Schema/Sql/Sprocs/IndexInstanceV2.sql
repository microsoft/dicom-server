/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance V2
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
--     @

--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV2
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

        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT

        -- Add lock so that the instance cannot be removed
        DECLARE @status TINYINT
        SELECT
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @status = Status
        FROM dbo.Instance WITH (HOLDLOCK)
        WHERE Watermark = @watermark

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exists', 1
        IF @status <> 1 -- Created
            THROW 50409, 'Instance has not yet been stored succssfully', 1

        -- Insert Extended Query Tags

        -- String Key tags
        IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagString WITH (HOLDLOCK) AS T
            USING
            (
                -- Locks tags in dbo.ExtendedQueryTag
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @stringExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                -- Only merge on extended query tag which is being adding.
                AND dbo.ExtendedQueryTag.TagStatus <> 2
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES
                (
                    S.TagKey,
                    S.TagValue,
                    @studyKey,
                    -- When TagLevel is not Study, we should fill SeriesKey
                    (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                    -- When TagLevel is Instance, we should fill InstanceKey
                    (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                    @watermark
                );
        END

        -- Long Key tags
        IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagLong WITH (HOLDLOCK) AS T
            USING
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @longExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus <> 2
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                 -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES
                (
                    S.TagKey,
                    S.TagValue,
                    @studyKey,
                    (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                    (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                    @watermark
                );
        END

        -- Double Key tags
        IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagDouble WITH (HOLDLOCK) AS T
            USING
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @doubleExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus <> 2
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES
                (
                    S.TagKey,
                    S.TagValue,
                    @studyKey,
                    (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                    (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                    @watermark
                );
        END

        -- DateTime Key tags
        IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagDateTime WITH (HOLDLOCK) AS T
            USING
            (
                SELECT input.TagKey, input.TagValue, input.TagValueUtc, input.TagLevel
                FROM @dateTimeExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus <> 2
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                 -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark, TagValueUtc)
                VALUES
                (
                    S.TagKey,
                    S.TagValue,
                    @studyKey,
                    (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                    (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                    @watermark,
                    S.TagValueUtc
                );
        END

        -- PersonName Key tags
        IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
        BEGIN
            MERGE INTO dbo.ExtendedQueryTagPersonName WITH (HOLDLOCK) AS T
            USING
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel
                FROM @personNameExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                AND dbo.ExtendedQueryTag.TagStatus <> 2
            ) AS S
            ON T.TagKey = S.TagKey
                AND T.StudyKey = @studyKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES
                (
                    S.TagKey,
                    S.TagValue,
                    @studyKey,
                    (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                    (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                    @watermark
                );
        END

    COMMIT TRANSACTION
END
