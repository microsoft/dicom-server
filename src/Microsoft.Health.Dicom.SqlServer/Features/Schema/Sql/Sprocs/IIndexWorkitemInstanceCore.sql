/***************************************************************************************/
-- STORED PROCEDURE
--    IIndexWorkitemInstanceCore
--
-- DESCRIPTION
--    Adds workitem query tag values
--    Unlike IndexInstance, IndexInstanceCore is not wrapped in a transaction and may be re-used by other
--    stored procedures whose logic may vary.
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @workitemKey
--         * Refers to WorkItemKey
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IIndexWorkitemInstanceCore
    @partitionKey                                                                INT = 1,
    @workitemKey                                                                 BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN

    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @newWatermark BIGINT

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
        INSERT dbo.ExtendedQueryTagString (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
        SELECT input.TagKey, input.TagValue, @partitionKey, @workitemKey, NULL, NULL, @newWatermark, @workitemResourceType
        FROM @stringExtendedQueryTags input
        INNER JOIN dbo.WorkitemQueryTag
        ON dbo.WorkitemQueryTag.TagKey = input.TagKey
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
        INSERT dbo.ExtendedQueryTagDateTime (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
        SELECT input.TagKey, input.TagValue, @partitionKey, @workitemKey, NULL, NULL, @newWatermark, @workitemResourceType
        FROM @dateTimeExtendedQueryTags input
        INNER JOIN dbo.WorkitemQueryTag
        ON dbo.WorkitemQueryTag.TagKey = input.TagKey
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
        INSERT dbo.ExtendedQueryTagPersonName (TagKey, TagValue, PartitionKey, SopInstanceKey1, SopInstanceKey2, SopInstanceKey3, Watermark, ResourceType)
        SELECT input.TagKey, input.TagValue, @partitionKey, @workitemKey, NULL, NULL, @newWatermark, @workitemResourceType
        FROM @personNameExtendedQueryTags input
        INNER JOIN dbo.WorkitemQueryTag
        ON dbo.WorkitemQueryTag.TagKey = input.TagKey
    END
END
