/***************************************************************************************/
-- STORED PROCEDURE
--     AddExtendedQueryTagError
--
-- DESCRIPTION
--    Adds an Extended Query Tag Error or Updates it if exists.
--
-- PARAMETERS
--     @tagKey
--         * The related extended query tag's key
--     @errorCode
--         * The error code
--     @watermark
--         * The watermark
--
-- RETURN VALUE
--     The tag key of the error added.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTagError
    @tagKey INT,
    @errorCode SMALLINT,
    @watermark BIGINT
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON
    BEGIN TRANSACTION

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

        --Check if instance with given watermark and Created status.
        IF NOT EXISTS (SELECT * FROM dbo.Instance WITH (UPDLOCK) WHERE Watermark = @watermark AND Status = 1)
            THROW 50404, 'Instance does not exist or has not been created.', 1;

        --Check if tag exists and in Adding status.
        IF NOT EXISTS (SELECT * FROM dbo.ExtendedQueryTag WITH (HOLDLOCK) WHERE TagKey = @tagKey AND TagStatus = 0)
            THROW 50404, 'Tag does not exist or is not being added.', 1;

        -- Add error
        DECLARE @addedCount SMALLINT
        SET @addedCount  = 1
        MERGE dbo.ExtendedQueryTagError WITH (HOLDLOCK) as XQTE
        USING (SELECT @tagKey TagKey, @errorCode ErrorCode, @watermark Watermark) as src
        ON src.TagKey = XQTE.TagKey AND src.WaterMark = XQTE.Watermark
        WHEN MATCHED THEN UPDATE
        SET CreatedTime = @currentDate,
            ErrorCode = @errorCode,
            @addedCount = 0
        WHEN NOT MATCHED THEN
            INSERT (TagKey, ErrorCode, Watermark, CreatedTime)
            VALUES (@tagKey, @errorCode, @watermark, @currentDate)
        OUTPUT INSERTED.TagKey;

        -- Disable query on the tag and update error count
        UPDATE dbo.ExtendedQueryTag
        SET QueryStatus = 0, ErrorCount = ErrorCount + @addedCount
        WHERE TagKey = @tagKey

    COMMIT TRANSACTION
END
