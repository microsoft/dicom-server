SET XACT_ABORT ON

BEGIN TRANSACTION
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     GetInstancesByWatermarkRange
--
-- DESCRIPTION
--     Get instances by given watermark range.
--
-- PARAMETERS
--     @startWatermark
--         * The inclusive start watermark.
--     @endWatermark
--         * The inclusive end watermark.
--     @status
--         * The instance status.
-- RETURN VALUE
--     The instance identifiers.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetInstancesByWatermarkRange(
    @startWatermark BIGINT,
    @endWatermark BIGINT,
    @status TINYINT
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM dbo.Instance
    WHERE Watermark BETWEEN @startWatermark AND @endWatermark
          AND Status = @status
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     AddExtendedQueryTags
--
-- DESCRIPTION
--    Add a list of extended query tags.
--
-- PARAMETERS
--     @extendedQueryTags
--         * The extended query tag list
--     @maxCount
--         * The max allowed extended query tag count
--     @ready
--         * Indicates whether the new query tags have been fully indexed
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @maxAllowedCount INT,
    @ready BIT = 0
)
AS
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        -- Check if total count exceed @maxCount
        -- HOLDLOCK to prevent adding queryTags from other transactions at same time.
        IF ((SELECT COUNT(*) FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)) + (SELECT COUNT(*) FROM @extendedQueryTags)) > @maxAllowedCount
             THROW 50409, 'extended query tags exceed max allowed count', 1

        -- Check if tag with same path already exist
        IF EXISTS(SELECT 1 FROM dbo.ExtendedQueryTag WITH(HOLDLOCK) INNER JOIN @extendedQueryTags input ON input.TagPath = dbo.ExtendedQueryTag.TagPath)
            THROW 50409, 'extended query tag(s) already exist', 2

        -- add to extended query tag table with status 0 (Adding)
        INSERT INTO dbo.ExtendedQueryTag
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        OUTPUT INSERTED.TagKey
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, @ready FROM @extendedQueryTags

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Reindex instance
--
-- DESCRIPTION
--    Reidex instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
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

/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.ReindexInstance
    @studyInstanceUid       VARCHAR(64),
    @seriesInstanceUid      VARCHAR(64),
    @sopInstanceUid         VARCHAR(64),               
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,    
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
    SET NOCOUNT    ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT
        DECLARE @watermark BIGINT

        -- Add lock so that the instance won't be removed
        DECLARE @status TINYINT
        SELECT
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @watermark = Watermark,
            @status = Status
        FROM dbo.Instance WITH (HOLDLOCK) 
        WHERE StudyInstanceUid = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid = @sopInstanceUid

        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance not exists', 1
        IF @status <> 1 -- Created
            THROW 50409, 'Instance is not been stored succssfully', 1

        -- Insert Extended Query Tags

        -- String Key tags
        IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
        BEGIN      
            MERGE INTO dbo.ExtendedQueryTagString AS T
            USING 
            (
                -- Locks tags in dbo.ExtendedQueryTag
                SELECT input.TagKey, input.TagValue, input.TagLevel 
                FROM @stringExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey
                -- Not merge on extended query tag which is being deleted.
                AND dbo.ExtendedQueryTag.TagStatus <> 2     
            ) AS S
            ON T.TagKey = S.TagKey        
                AND T.StudyKey = @studyKey
                -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey      
                -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN 
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES(
                S.TagKey,
                S.TagValue,
                @studyKey,
                -- When TagLevel is not Study, we should fill SeriesKey
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
                -- When TagLevel is Instance, we should fill InstanceKey
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark);        
        END

        -- Long Key tags
        IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
        BEGIN      
            MERGE INTO dbo.ExtendedQueryTagLong AS T
            USING 
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel 
                FROM @longExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
                AND dbo.ExtendedQueryTag.TagStatus <> 2     
            ) AS S
            ON T.TagKey = S.TagKey        
                AND T.StudyKey = @studyKey            
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN 
                 -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN 
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES(
                S.TagKey,
                S.TagValue,
                @studyKey,            
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark);        
        END

        -- Double Key tags
        IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
        BEGIN      
            MERGE INTO dbo.ExtendedQueryTagDouble AS T
            USING 
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel 
                FROM @doubleExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
                AND dbo.ExtendedQueryTag.TagStatus <> 2     
            ) AS S
            ON T.TagKey = S.TagKey        
                AND T.StudyKey = @studyKey            
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN 
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN 
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES(
                S.TagKey,
                S.TagValue,
                @studyKey,            
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark);        
        END

        -- DateTime Key tags
        IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
        BEGIN      
            MERGE INTO dbo.ExtendedQueryTagDateTime AS T
            USING 
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel 
                FROM @dateTimeExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
                AND dbo.ExtendedQueryTag.TagStatus <> 2     
            ) AS S
            ON T.TagKey = S.TagKey        
                AND T.StudyKey = @studyKey            
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN 
                 -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN 
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES(
                S.TagKey,
                S.TagValue,
                @studyKey,            
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark);        
        END

        -- PersonName Key tags
        IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
        BEGIN      
            MERGE INTO dbo.ExtendedQueryTagPersonName AS T
            USING 
            (
                SELECT input.TagKey, input.TagValue, input.TagLevel 
                FROM @personNameExtendedQueryTags input
                INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
                ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
                AND dbo.ExtendedQueryTag.TagStatus <> 2     
            ) AS S
            ON T.TagKey = S.TagKey        
                AND T.StudyKey = @studyKey            
                AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
                AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
            WHEN MATCHED THEN 
                -- When index already exist, update only when watermark is newer
                UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
            WHEN NOT MATCHED THEN 
                INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
                VALUES(
                S.TagKey,
                S.TagValue,
                @studyKey,            
                (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
                (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
                @watermark);        
        END

    COMMIT TRANSACTION
GO

COMMIT TRANSACTION
GO
