/***************************************************************************************/
-- STORED PROCEDURE
--    UpdateIndexWorkitemInstanceCore
--
-- DESCRIPTION
--    Updates workitem query tag values.
--    
-- PARAMETERS
--     @workitemKey
--         * Refers to WorkItemKey
--     @partitionKey
--         * Refers to Partition Key
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateIndexWorkitemInstanceCore
    @workitemKey                                                                 BIGINT,
    @partitionKey                                                                INT,
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

        UPDATE ets
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        FROM dbo.ExtendedQueryTagString AS ets
        INNER JOIN @stringExtendedQueryTags AS input
            ON ets.TagKey = input.TagKey
        WHERE
            SopInstanceKey1 = @workitemKey
            AND ResourceType = @workitemResourceType
            AND PartitionKey = @partitionKey
            AND ets.TagValue <> input.TagValue

    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN

        UPDATE etdt
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        FROM dbo.ExtendedQueryTagDateTime AS etdt
        INNER JOIN @dateTimeExtendedQueryTags AS input
            ON etdt.TagKey = input.TagKey
        WHERE
            SopInstanceKey1 = @workitemKey
            AND ResourceType = @workitemResourceType
            AND PartitionKey = @partitionKey
            AND etdt.TagValue <> input.TagValue

    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN

        UPDATE etpn
        SET
            TagValue = input.TagValue,
            Watermark = @newWatermark
        FROM dbo.ExtendedQueryTagPersonName AS etpn
        INNER JOIN @personNameExtendedQueryTags AS input
            ON etpn.TagKey = input.TagKey
        WHERE
            SopInstanceKey1 = @workitemKey
            AND ResourceType = @workitemResourceType
            AND PartitionKey = @partitionKey
            AND etpn.TagValue <> input.TagValue

    END
END
GO
