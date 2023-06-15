/***************************************************************************************/
-- STORED PROCEDURE
--    IIndexInstanceCoreV9
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @studyKey
--         * The internal key for the study
--     @seriesKey
--         * The internal key for the series
--     @instanceKey
--         * The internal key for the instance
--     @watermark
--         * The DICOM instance watermark
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
--     @resourceType
--         * The resource type that owns these tags: 0 = Image, 1 = Workitem. Default is Image
--
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCoreV9
    @partitionKey                                                                INT = 1,
    @studyKey                                                                    BIGINT,
    @seriesKey                                                                   BIGINT,
    @instanceKey                                                                 BIGINT,
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    -- Note that it is the responsibility of the callers to lock the appropriate indexes to prevent incorrect updates.
    DECLARE @resourceType TINYINT = 0

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagString AS T
        USING
        (
            -- Locks tags in dbo.ExtendedQueryTag
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @stringExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @studyKey,
                -- When TagLevel is not Study, we should fill SeriesKey
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                -- When TagLevel is Instance, we should fill InstanceKey
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                @resourceType
            );
    END

    -- Long Key tags
    IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagLong AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @longExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                @resourceType
            );
    END

    -- Double Key tags
    IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagDouble AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @doubleExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                @resourceType
            );
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagDateTime AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagValueUtc, input.TagLevel
            FROM @dateTimeExtendedQueryTags input
           INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, TagValueUtc, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                S.TagValueUtc,
                @resourceType
            );
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
        MERGE INTO dbo.ExtendedQueryTagPersonName AS T
        USING
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel
            FROM @personNameExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD)
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.ResourceType = @resourceType
            AND T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @studyKey
            AND ISNULL(T.SopInstanceKey2, @seriesKey) = @seriesKey
            AND ISNULL(T.SopInstanceKey3, @instanceKey) = @instanceKey
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = ISNULL(S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @studyKey,
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark,
                @resourceType
            );
    END
END
