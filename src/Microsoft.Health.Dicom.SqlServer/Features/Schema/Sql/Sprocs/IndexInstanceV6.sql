/***************************************************************************************/
-- STORED PROCEDURE
--    Index instance V6
--
-- DESCRIPTION
--    Adds or updates the various extended query tag indices for a given DICOM instance.
--
-- PARAMETERS
--     @watermark
--         * The Dicom instance watermark.
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
-- RETURN VALUE
--     None
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.IndexInstanceV6
    @watermark                                                                   BIGINT,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1         READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1             READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1         READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2     READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT    ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT

        -- Add lock so that the instance cannot be removed
        DECLARE @status TINYINT
        SELECT
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @status = Status
        FROM dbo.Instance WITH (HOLDLOCK)
        WHERE Watermark = @watermark

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exists', 1
        IF @status <> 1 -- Created
            THROW 50409, 'Instance has not yet been stored succssfully', 1

        -- Insert Extended Query Tags

        -- String Key tags
        BEGIN TRY

            EXEC dbo.IIndexInstanceCore
                @studyKey,
                @seriesKey,
                @instanceKey,
                @watermark,
                @stringExtendedQueryTags,
                @longExtendedQueryTags,
                @doubleExtendedQueryTags,
                @dateTimeExtendedQueryTags,
                @personNameExtendedQueryTags

        END TRY
        BEGIN CATCH

            THROW

        END CATCH

    COMMIT TRANSACTION
END
