SET XACT_ABORT ON

BEGIN TRANSACTION
GO
CREATE OR ALTER PROCEDURE dbo.GetIndexedFileMetrics
    AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
SELECT TotalIndexedFileCount=COUNT_BIG(*),
       TotalIndexedBytes=SUM(ContentLength)
FROM   dbo.FileProperty;
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
    
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_FileProperty_ContentLength'
        AND Object_id = OBJECT_ID('dbo.FileProperty')
)
BEGIN
    CREATE NONCLUSTERED INDEX IXC_FileProperty_ContentLength ON dbo.FileProperty
    (
    ContentLength
    ) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON)
END
GO

COMMIT TRANSACTION
