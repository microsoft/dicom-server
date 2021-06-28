
/*************************************************************
Wrapping up in a transaction except CREATE FULLTEXT INDEX which is non-transactional script. Since there are no slow scripts(all the statements i.e. CREATE TABLE/INDEX/STORED PROC and ALTER STORED PROC are faster) so keeping all in one transaction.
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
**************************************************************/
SET XACT_ABORT ON
BEGIN TRANSACTION

/*************************************************************
    Reindex State Table. 
    Reindex state on each extended query tag.
**************************************************************/
IF NOT EXISTS (
    SELECT * 
    FROM sys.tables
    WHERE name = 'ReindexState')
BEGIN
    CREATE TABLE dbo.ReindexState (
        TagKey                  INT                  NOT NULL,
        OperationId             VARCHAR(40)          NULL,
        ReindexStatus           TINYINT              NOT NULL, -- 0: Processing, 1: Paused, 2: Completed
        StartWatermark          BIGINT               NULL, 
        EndWatermark            BIGINT               NULL, 
    ) WITH (DATA_COMPRESSION = PAGE)
END
GO

IF NOT EXISTS (
    SELECT * 
	FROM sys.indexes 
	WHERE name='IXC_ReindexState' AND object_id = OBJECT_ID('dbo.ReindexState'))
BEGIN
-- One tag should have and only have one entry.
    CREATE UNIQUE CLUSTERED INDEX IXC_ReindexState on dbo.ReindexState
    (
        TagKey
    )    
END
GO

/*************************************************************
    Table valued parameter to provide tag keys to PrepareReindexing
*************************************************************/
IF TYPE_ID(N'dbo.PrepareReindexingTableType_1') IS NULL
BEGIN
    CREATE TYPE  dbo.PrepareReindexingTableType_1 AS TABLE
    (
        TagKey                     INT -- TagKey
    )
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    PrepareReindexing
--
-- DESCRIPTION
--    Prepare reindexing on tags with operation id.
--
-- PARAMETERS
--     @@tagKeys
--         * The tag keys.
--     @@operationId
--         * The operation id
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.PrepareReindexing(
    @tagKeys dbo.PrepareReindexingTableType_1 READONLY,
    @operationId VARCHAR(40)
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN  TRANSACTION

        -- Validate: tagkeys should be in extendedQueryTag with status of Adding
        IF
        (
            (
                SELECT COUNT(1) FROM @tagKeys
            )
            <>
            (
                SELECT COUNT(1) FROM dbo.ExtendedQueryTag E WITH (REPEATABLEREAD) 
                INNER JOIN @tagKeys I
                ON E.TagKey = I.TagKey
                AND E.TagStatus = 0 -- 0 - Adding, 1 - Ready, 2 - Deleting
            )
        )
            THROW 50412, 'Not all tags are valid for reindexing', 1
        
        -- Add tagKey and operationId combination to ReindexState table
        INSERT INTO dbo.ReindexState
        (TagKey, OperationId, ReindexStatus, StartWatermark, EndWatermark)
        SELECT  TagKey,
                @operationId,
                0, -- 0 - Processing, 1 - Paused, 2 - Completed
                ( SELECT MIN(Watermark) FROM dbo.Instance ),
                ( SELECT MAX(Watermark) FROM dbo.Instance )
        FROM @tagKeys
    
        -- Join with ExtendeQueryTag to return all information
        SELECT  E.TagKey,
                E.TagPath,
                E.TagVR,
                E.TagPrivateCreator,
                E.TagLevel,
                E.TagStatus,
                R.OperationId,
                R.ReindexStatus,
                R.StartWatermark,
                R.EndWatermark
        FROM    dbo.ExtendedQueryTag E
        INNER JOIN @tagKeys I
        ON      E.TagKey = I.TagKey
        INNER JOIN dbo.ReindexState R
        ON      E.TagKey = R.TagKey
    
    COMMIT TRANSACTION
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
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @initStatus TINYINT,  --- 0 - Adding, 1 - Ready, 2 - Deleting
    @maxAllowedCount INT
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
        SELECT TagKey 
        FROM dbo.ExtendedQueryTag WITH(HOLDLOCK) 
        INNER JOIN @extendedQueryTags input 
        ON input.TagPath = dbo.ExtendedQueryTag.TagPath 
	    
        IF @@ROWCOUNT <> 0
            THROW 50409, 'extended query tag(s) already exist', 2 

        -- add to extended query tag table with status 1(Ready)
        INSERT INTO dbo.ExtendedQueryTag 
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, @initStatus FROM @extendedQueryTags
        
    COMMIT TRANSACTION
GO

COMMIT TRANSACTION
GO
