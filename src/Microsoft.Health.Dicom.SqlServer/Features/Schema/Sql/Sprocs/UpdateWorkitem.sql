/*************************************************************
    Stored procedure to UpdateWorkitem a workitem procedure step state.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateWorkitem
--
-- DESCRIPTION
--     Update a UPS-RS Workitem.
--
-- PARAMETERS
--     @partitionKey
--         * The system identifier of the data partition.
--     @workitemUid
--         * The workitem UID.
--     @stringExtendedQueryTags
--         * String extended query tag data
--     @dateTimeExtendedQueryTags
--         * DateTime extended query tag data
--     @personNameExtendedQueryTags
--         * PersonName extended query tag data
-- RETURN VALUE
--     None
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.UpdateWorkitem
    @partitionKey                   INT,
    @workitemUid                    VARCHAR(64),

    @stringExtendedQueryTags        dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags      dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags    dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @workitemResourceType TINYINT = 1
    DECLARE @newWatermark BIGINT

    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

    DECLARE @workitemKey BIGINT

    SELECT @workitemKey = WorkitemKey
    FROM dbo.Workitem
    WHERE PartitionKey = @partitionKey
        AND WorkitemUid = @workitemUid

    IF @@ROWCOUNT = 0
        THROW 50413, 'Workitem does not exist', 1;

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN
		WITH InputCTE AS (
			SELECT 
				input.TagValue,
				input.TagKey
			FROM 
				@stringExtendedQueryTags input
				INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey
		)
        UPDATE dbo.ExtendedQueryTagString
        SET
            TagValue = cte.TagValue,
            Watermark = @newWatermark
        FROM
			dbo.ExtendedQueryTagString t
			INNER JOIN InputCTE cte ON cte.TagKey = t.TagKey
		WHERE
            SopInstanceKey1 = @workitemKey
			AND PartitionKey = @partitionKey
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN
		WITH InputCTE AS (
			SELECT 
				input.TagValue,
				input.TagKey
			FROM 
				@dateTimeExtendedQueryTags input
				INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey
		)
        UPDATE dbo.ExtendedQueryTagDateTime
        SET
            TagValue = cte.TagValue,
            Watermark = @newWatermark
        FROM
			dbo.ExtendedQueryTagDateTime t
			INNER JOIN InputCTE cte ON cte.TagKey = t.TagKey
		WHERE
            SopInstanceKey1 = @workitemKey
			AND PartitionKey = @partitionKey
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN
		WITH InputCTE AS (
			SELECT 
				input.TagValue,
				input.TagKey
			FROM 
				@personNameExtendedQueryTags input
				INNER JOIN dbo.WorkitemQueryTag ON dbo.WorkitemQueryTag.TagKey = input.TagKey
		)
        UPDATE dbo.ExtendedQueryTagPersonName
        SET
            TagValue = cte.TagValue,
            Watermark = @newWatermark
        FROM
			dbo.ExtendedQueryTagPersonName t
			INNER JOIN InputCTE cte ON cte.TagKey = t.TagKey
		WHERE
            SopInstanceKey1 = @workitemKey
			AND PartitionKey = @partitionKey
    END

    COMMIT TRANSACTION

    SELECT @workitemKey

END
