/***************************************************************************************/
-- STORED PROCEDURE
--    IIndexInstanceCoreV8
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @sopInstanceKey1
--         * Refers to either StudyKey or WorkItemKey depending on ResourceType
--     @sopInstanceKey2
--         * Refers to SeriesKey if ResourceType is Image else NULL
--     @sopInstanceKey3
--         * Refers to InstanceKey if ResourceType is Image else NULL
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
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexInstanceCoreV8
    @partitionKey                                                                INT = 1,
    @sopInstanceKey1                                                             BIGINT,
    @sopInstanceKey2                                                             BIGINT,
    @sopInstanceKey3                                                             BIGINT,
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
    @resourceType                                                                TINYINT = 0
AS
BEGIN
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
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
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
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
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
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
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
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, TagValueUtc, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                S.TagValueUtc,
                @resourceType
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
            -- Only merge on extended query tag which is being added
            AND dbo.ExtendedQueryTag.TagStatus <> 2
        ) AS S
        ON T.TagKey = S.TagKey
            AND T.PartitionKey = @partitionKey
            AND T.SopInstanceKey1 = @sopInstanceKey1
            -- Null SopInstanceKey2 indicates a Study level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey2, @sopInstanceKey2) = @sopInstanceKey2
            -- Null SopInstanceKey3 indicates a Study/Series level or Workitem tag, no need to compare further
            AND ISNULL(T.SopInstanceKey3, @sopInstanceKey3) = @sopInstanceKey3
        WHEN MATCHED AND @watermark > T.Watermark THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = @watermark, T.TagValue = S.TagValue
        WHEN NOT MATCHED THEN
            INSERT (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
            VALUES
            (
                S.TagKey,
                S.TagValue,
                @partitionKey,
                @sopInstanceKey1,
                -- When TagLevel is not Study, we should fill SopInstanceKey2 (Series)
                (CASE WHEN S.TagLevel <> 2 THEN @sopInstanceKey2 ELSE NULL END),
                -- When TagLevel is Instance, we should fill SopInstanceKey3 (Instance)
                (CASE WHEN S.TagLevel = 0 THEN @sopInstanceKey3 ELSE NULL END),
                @watermark,
                @resourceType
            );
    END
END
