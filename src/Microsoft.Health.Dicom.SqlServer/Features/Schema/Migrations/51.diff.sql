SET XACT_ABORT ON

BEGIN TRANSACTION
      
/*************************************************************
    Table Updates
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   (NAME = 'ContentLength')
        AND Object_id = OBJECT_ID('dbo.FileProperty')
)
BEGIN
ALTER TABLE dbo.FileProperty 
    ADD ContentLength BIGINT NOT NULL;
END

GO

/*************************************************************
    FilePropertyTableType TableType
    To use when inserting rows of FileProperty
**************************************************************/
IF TYPE_ID(N'FilePropertyTableType_2') IS NULL
BEGIN
CREATE TYPE dbo.FilePropertyTableType_2  AS TABLE
    (
    Watermark       BIGINT          NOT NULL INDEX IXC_FilePropertyTableType_2 CLUSTERED,
    FilePath        NVARCHAR (4000) NOT NULL,
    ETag            NVARCHAR (4000) NOT NULL,
    ContentLength   BIGINT NOT NULL
    )
END

GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatusV6
--
-- DESCRIPTION
--     Updates a DICOM instance status, which allows for consistency during indexing.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark.
--     @status
--         * The new status to update to.
--     @maxTagKey
--         * Optional max ExtendedQueryTag key
--     @hasFrameMetadata
--         * Optional flag to indicate frame metadata existance
--     @path
--         * path to dcm blob file
--     @eTag
--         * eTag of upload blob operation
--     @contentLength
--         * content length of uploaded blob
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV51
    @partitionKey               INT,
    @studyInstanceUid           VARCHAR(64),
    @seriesInstanceUid          VARCHAR(64),
    @sopInstanceUid             VARCHAR(64),
    @watermark                  BIGINT,
    @status                     TINYINT,
    @maxTagKey                  INT = NULL,
    @hasFrameMetadata           BIT = 0,
    @path                       VARCHAR(4000) = NULL,
    @eTag                       VARCHAR(4000) = NULL,
    @contentLength              BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    -- This check ensures the client is not potentially missing 1 or more query tags that may need to be indexed.
    -- Note that if @maxTagKey is NULL, < will always return UNKNOWN.
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0) FROM dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    DECLARE @instanceKey BIGINT

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @CurrentDate, HasFrameMetadata = @hasFrameMetadata, @instanceKey = InstanceKey
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND Watermark = @watermark

    -- The instance does not exist. Perhaps it was deleted?
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1
        
    -- Insert to FileProperty when specified params passed in
    IF (@path IS NOT NULL AND @eTag IS NOT NULL AND @watermark IS NOT NULL)
        INSERT INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag, ContentLength)
        VALUES                       (@instanceKey, @watermark, @path, @eTag, @contentLength)

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Timestamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark, FilePath)
    VALUES
        (@currentDate, 0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark, @path)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid

    COMMIT TRANSACTION
END
GO


/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     EndUpdateInstanceV51
--
-- DESCRIPTION
--     Bulk update all instances in a study, creates new entry in changefeed and fileProperty for each new file added.
--
-- PARAMETERS
--     @partitionKey
--         * The partition key.
--     @studyInstanceUid
--         * The study instance UID.
--     @patientId
--         * The Id of the patient.
--     @patientName
--         * The name of the patient.
--     @patientBirthDate
--         * The patient's birth date.
--     @insertFileProperties
--         * A table type containing the file properties to insert.
--
-- RETURN VALUE
--     None
--

CREATE OR ALTER PROCEDURE dbo.EndUpdateInstanceV51
    @partitionKey INT,
    @studyInstanceUid VARCHAR(64),
    @patientId NVARCHAR(64) = NULL,
    @patientName NVARCHAR(325) = NULL,
    @patientBirthDate DATE = NULL,
    @insertFileProperties dbo.FilePropertyTableType_2 READONLY,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
        DECLARE @resourceType TINYINT = 0
        DECLARE @studyKey BIGINT
        DECLARE @maxWatermark BIGINT

        CREATE TABLE #UpdatedInstances
               (PartitionKey INT,
                StudyInstanceUid VARCHAR(64),
                SeriesInstanceUid VARCHAR(64),
                SopInstanceUid VARCHAR(64),
                Watermark BIGINT,
                OriginalWatermark BIGINT,
                InstanceKey BIGINT)

        DELETE FROM #UpdatedInstances

        UPDATE dbo.Instance
        SET LastStatusUpdatedDate = @currentDate,
            OriginalWatermark = ISNULL(OriginalWatermark, Watermark),
            Watermark = NewWatermark,
            NewWatermark = NULL
        OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, inserted.Watermark, inserted.OriginalWatermark, deleted.InstanceKey INTO #UpdatedInstances
        WHERE PartitionKey = @partitionKey
          AND StudyInstanceUid = @studyInstanceUid
          AND Status = 1
          AND NewWatermark IS NOT NULL

        -- Create index on temp table so we can join on watermark when we need to update change feed with file path.
        IF NOT EXISTS (SELECT *
                       FROM   tempdb.sys.indexes
                       WHERE  name = 'IXC_UpdatedInstances')
            CREATE UNIQUE INDEX IXC_UpdatedInstances ON #UpdatedInstances (Watermark)

        -- Create index on temp table so we can join on instance key and watermark when we need to insert file
        -- properties when instance key alone is does not specify a unique row on the table
        IF NOT EXISTS (SELECT *
                       FROM   tempdb.sys.indexes
                       WHERE  name = 'IXC_UpdatedInstanceKeyWatermark')
        CREATE UNIQUE CLUSTERED INDEX IXC_UpdatedInstanceKeyWatermark ON #UpdatedInstances (InstanceKey, OriginalWatermark)

        -- Only updating patient information in a study
        UPDATE dbo.Study
        SET PatientId = ISNULL(@patientId, PatientId),
            PatientName = ISNULL(@patientName, PatientName),
            PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate),
            @studyKey = StudyKey
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid

        -- The study does not exist. May be deleted
        IF @@ROWCOUNT = 0
            THROW 50404, 'Study does not exist', 1

        -- Delete from file properties any rows with "stale" watermarks if we will be inserting new ones
        IF EXISTS (SELECT 1 FROM @insertFileProperties)
        DELETE FP
        FROM dbo.FileProperty as FP
        INNER JOIN #UpdatedInstances U
        ON U.InstanceKey = FP.InstanceKey
        WHERE U.OriginalWatermark != FP.Watermark

        -- Insert new file properties from added blobs, @insertFileProperties will be empty when external store not
        -- enabled
        INSERT INTO dbo.FileProperty
        (InstanceKey, Watermark, FilePath, ETag, ContentLength)
        SELECT U.InstanceKey, I.Watermark, I.FilePath, I.ETag, I.ContentLength
        FROM @insertFileProperties I
        INNER JOIN #UpdatedInstances U
        ON U.Watermark = I.Watermark

        SELECT
            @maxWatermark = max(Watermark)
        FROM #UpdatedInstances

        -- Update extended query tags value if any
        BEGIN TRY
            EXEC dbo.IIndexInstanceCoreV9
                @partitionKey,
                @studyKey,
                null, -- passing null to series key and instance
                null,
                @maxWatermark,
                @stringExtendedQueryTags,
                @longExtendedQueryTags,
                @doubleExtendedQueryTags,
                @dateTimeExtendedQueryTags,
                @personNameExtendedQueryTags
        END TRY
        BEGIN CATCH
            THROW
        END CATCH

        -- Insert into change feed table for update action type
        INSERT INTO dbo.ChangeFeed
        (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
        SELECT 2, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
        FROM #UpdatedInstances

        -- Update existing instance currentWatermark to latest and update file path
        UPDATE C
        SET CurrentWatermark = U.Watermark, FilePath = I.FilePath
        FROM dbo.ChangeFeed C
        JOIN #UpdatedInstances U
        ON C.PartitionKey = U.PartitionKey
            AND C.StudyInstanceUid = U.StudyInstanceUid
            AND C.SeriesInstanceUid = U.SeriesInstanceUid
            AND C.SopInstanceUid = U.SopInstanceUid
        LEFT OUTER JOIN @insertFileProperties I
        ON I.Watermark = U.Watermark

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION
