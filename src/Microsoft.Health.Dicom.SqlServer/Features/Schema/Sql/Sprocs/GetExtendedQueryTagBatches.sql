/***************************************************************************************/
-- STORED PROCEDURE
--     GetExtendedQueryTagBatches
--
-- DESCRIPTION
--     Divides up the extended query data into a configurable number of batches based on watermark
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @batchCount
--         * The desired number of batches. Actual number may be smaller.
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
--     @tagKey
--         * The extended query tag key
--
-- RETURN VALUE
--     The batches as defined by their inclusive minimum and maximum values.
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
