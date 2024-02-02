/**************************************************************/
--
-- STORED PROCEDURE
--     DeleteExtendedQueryTagDataByWatermarkRange
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Deletes the extended query tag data in the provided watermark range
--
-- PARAMETERS
--     @startWatermark
--         * The inclusive start watermark.
--     @endWatermark
--         * The inclusive end watermark.
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
--     @tagKey
--         * The extended query tag key
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagDataByWatermarkRange
    @startWatermark BIGINT,
    @endWatermark BIGINT,
    @dataType TINYINT,
    @tagKey INT
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

        DECLARE @imageResourceType TINYINT = 0

        IF @dataType = 0
            DELETE FROM dbo.ExtendedQueryTagString WHERE TagKey = @tagKey AND ResourceType = @imageResourceType AND Watermark BETWEEN @startWatermark AND @endWatermark
        ELSE IF @dataType = 1
            DELETE FROM dbo.ExtendedQueryTagLong WHERE TagKey = @tagKey AND ResourceType = @imageResourceType AND Watermark BETWEEN @startWatermark AND @endWatermark
        ELSE IF @dataType = 2
            DELETE FROM dbo.ExtendedQueryTagDouble WHERE TagKey = @tagKey AND ResourceType = @imageResourceType AND Watermark BETWEEN @startWatermark AND @endWatermark
        ELSE IF @dataType = 3
            DELETE FROM dbo.ExtendedQueryTagDateTime WHERE TagKey = @tagKey AND ResourceType = @imageResourceType AND Watermark BETWEEN @startWatermark AND @endWatermark
        ELSE
            DELETE FROM dbo.ExtendedQueryTagPersonName WHERE TagKey = @tagKey AND ResourceType = @imageResourceType AND Watermark BETWEEN @startWatermark AND @endWatermark

    COMMIT TRANSACTION

    BEGIN TRANSACTION

        DELETE FROM dbo.ExtendedQueryTagError
        WHERE TagKey = @tagKey AND Watermark BETWEEN @startWatermark AND @endWatermark

    COMMIT TRANSACTION
END
