SET XACT_ABORT ON

BEGIN TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     Delete extended query tag entry
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagEntry (
    @tagKey INT
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    -- Delete tag
    DELETE FROM dbo.ExtendedQueryTag
    WHERE TagKey = @tagKey
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     Deletes the extended query tag data in the provided watermark range
--
/***************************************************************************************/
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
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     Gets the batches for extended query tag by watermark range
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetExtendedQueryTagBatches
    @batchSize INT,
    @batchCount INT,
    @dataType TINYINT,
    @tagKey INT
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @imageResourceType TINYINT = 0

    IF @dataType = 0
        SELECT
            MIN(Watermark) AS MinWatermark,
            MAX(Watermark) AS MaxWatermark
        FROM
        (
            SELECT TOP (@batchSize * @batchCount)
                U.Watermark,
                (ROW_NUMBER() OVER(ORDER BY U.Watermark DESC) - 1) / @batchSize AS Batch
            FROM
			(
				SELECT Watermark
				FROM dbo.ExtendedQueryTagString
				WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
				UNION
				SELECT Watermark
				FROM dbo.ExtendedQueryTagError
				WHERE TagKey = @tagKey
			) as U
        ) AS I
        GROUP BY Batch
        ORDER BY Batch ASC
    ELSE IF @dataType = 1
        SELECT
            MIN(Watermark) AS MinWatermark,
            MAX(Watermark) AS MaxWatermark
        FROM
        (
            SELECT TOP (@batchSize * @batchCount)
                U.Watermark,
                (ROW_NUMBER() OVER(ORDER BY U.Watermark DESC) - 1) / @batchSize AS Batch
            FROM
			(
				SELECT Watermark
				FROM dbo.ExtendedQueryTagLong
				WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
				UNION
				SELECT Watermark
				FROM dbo.ExtendedQueryTagError
				WHERE TagKey = @tagKey
			) as U
        ) AS I
        GROUP BY Batch
        ORDER BY Batch ASC
    ELSE IF @dataType = 2
        SELECT
            MIN(Watermark) AS MinWatermark,
            MAX(Watermark) AS MaxWatermark
        FROM
        (
            SELECT TOP (@batchSize * @batchCount)
                U.Watermark,
                (ROW_NUMBER() OVER(ORDER BY U.Watermark DESC) - 1) / @batchSize AS Batch
            FROM
			(
				SELECT Watermark
				FROM dbo.ExtendedQueryTagDouble
				WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
				UNION
				SELECT Watermark
				FROM dbo.ExtendedQueryTagError
				WHERE TagKey = @tagKey
			) as U
        ) AS I
        GROUP BY Batch
        ORDER BY Batch ASC
    ELSE IF @dataType = 3
        SELECT
            MIN(Watermark) AS MinWatermark,
            MAX(Watermark) AS MaxWatermark
        FROM
        (
            SELECT TOP (@batchSize * @batchCount)
                U.Watermark,
                (ROW_NUMBER() OVER(ORDER BY U.Watermark DESC) - 1) / @batchSize AS Batch
            FROM
			(
				SELECT Watermark
				FROM dbo.ExtendedQueryTagDateTime
				WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
				UNION
				SELECT Watermark
				FROM dbo.ExtendedQueryTagError
				WHERE TagKey = @tagKey
			) as U
        ) AS I
        GROUP BY Batch
        ORDER BY Batch ASC
    ELSE
        SELECT
            MIN(Watermark) AS MinWatermark,
            MAX(Watermark) AS MaxWatermark
        FROM
        (
            SELECT TOP (@batchSize * @batchCount)
                U.Watermark,
                (ROW_NUMBER() OVER(ORDER BY U.Watermark DESC) - 1) / @batchSize AS Batch
            FROM
			(
				SELECT Watermark
				FROM dbo.ExtendedQueryTagPersonName
				WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
				UNION
				SELECT Watermark
				FROM dbo.ExtendedQueryTagError
				WHERE TagKey = @tagKey
			) as U
        ) AS I
        GROUP BY Batch
        ORDER BY Batch ASC
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     Update the status of the extended query tag to Deleting
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateExtendedQueryTagStatusToDelete
    @tagKey INT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION

        DECLARE @tagStatus TINYINT

        SELECT @tagStatus = TagStatus
        FROM dbo.ExtendedQueryTag WITH(XLOCK)
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1

        -- check if status is Ready or Adding
        IF @tagStatus = 2
            THROW 50412, 'extended query tag is not in Ready or Adding status', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION
