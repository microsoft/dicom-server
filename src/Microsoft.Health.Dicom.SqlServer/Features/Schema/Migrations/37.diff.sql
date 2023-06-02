SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    File Property Table
    Stores file properties of a given instance
**************************************************************/
IF NOT EXISTS (
    SELECT *
    FROM sys.tables
    WHERE name = 'FileProperty')
CREATE TABLE dbo.FileProperty (
    InstanceKey BIGINT          NOT NULL,
    Watermark   BIGINT          NOT NULL,
    FilePath    NVARCHAR (4000) NOT NULL,
    ETag        NVARCHAR (4000) NOT NULL
)
WITH (DATA_COMPRESSION = PAGE)
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name='IXC_FileProperty' AND object_id = OBJECT_ID('dbo.FileProperty'))
CREATE UNIQUE CLUSTERED INDEX IXC_FileProperty
    ON dbo.FileProperty(InstanceKey, Watermark)
    WITH (DATA_COMPRESSION = PAGE, ONLINE=ON)
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
--     @instanceKey
--         * The instance key.
--     @filePath
--         * path to dcm blob file
--     @eTag
--         * eTag of upload blob operation
--
-- RETURN VALUE
--     None
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceStatusV37
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @watermark BIGINT, @status TINYINT, @maxTagKey INT=NULL, @hasFrameMetadata BIT=0, @path VARCHAR (4000)=NULL, @eTag VARCHAR (4000)=NULL, @instanceKey BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF @maxTagKey < (SELECT ISNULL(MAX(TagKey), 0)
                     FROM   dbo.ExtendedQueryTag WITH (HOLDLOCK))
        THROW 50409, 'Max extended query tag key does not match', 10;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    UPDATE dbo.Instance
    SET    Status                = @status,
           LastStatusUpdatedDate = @currentDate,
           HasFrameMetadata      = @hasFrameMetadata
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid
           AND Watermark = @watermark;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Instance does not exist', 1;
    IF (@instanceKey IS NOT NULL AND @path IS NOT NULL)
        INSERT  INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
        VALUES                       (@instanceKey, @watermark, @path, @eTag);
    INSERT  INTO dbo.ChangeFeed (Timestamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES                     (@currentDate, 0, @partitionKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark);
    UPDATE dbo.ChangeFeed
    SET    CurrentWatermark = @watermark
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid
           AND SeriesInstanceUid = @seriesInstanceUid
           AND SopInstanceUid = @sopInstanceUid;
    COMMIT TRANSACTION;
END
GO

/*************************************************************
    Stored procedure for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstanceV37
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Adds a DICOM instance, now with partition.
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @patientId
--         * The Id of the patient.
--     @patientName
--         * The name of the patient.
--     @referringPhysicianName
--         * The referring physician name.
--     @studyDate
--         * The study date.
--     @studyDescription
--         * The study description.
--     @accessionNumber
--         * The accession number associated for the study.
--     @modality
--         * The modality associated for the series.
--     @performedProcedureStepStartDate
--         * The date when the procedure for the series was performed.
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
--     @initialStatus
--         * Initial status of the row
--     @transferSyntaxUid
--         * Instance transfer syntax UID

-- RETURN VALUE
--     The watermark (version) and instanceKey.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AddInstanceV37
@partitionKey INT, @studyInstanceUid VARCHAR (64), @seriesInstanceUid VARCHAR (64), @sopInstanceUid VARCHAR (64), @patientId NVARCHAR (64), @patientName NVARCHAR (325)=NULL, @referringPhysicianName NVARCHAR (325)=NULL, @studyDate DATE=NULL, @studyDescription NVARCHAR (64)=NULL, @accessionNumber NVARCHAR (64)=NULL, @modality NVARCHAR (16)=NULL, @performedProcedureStepStartDate DATE=NULL, @patientBirthDate DATE=NULL, @manufacturerModelName NVARCHAR (64)=NULL, @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY, @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY, @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY, @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY, @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY, @initialStatus TINYINT, @transferSyntaxUid VARCHAR (64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
        DECLARE @existingStatus AS TINYINT;
        DECLARE @newWatermark AS BIGINT;
        DECLARE @studyKey AS BIGINT;
        DECLARE @seriesKey AS BIGINT;
        DECLARE @instanceKey AS BIGINT;
        SELECT @existingStatus = Status
        FROM   dbo.Instance
        WHERE  PartitionKey = @partitionKey
               AND StudyInstanceUid = @studyInstanceUid
               AND SeriesInstanceUid = @seriesInstanceUid
               AND SopInstanceUid = @sopInstanceUid;
        IF @@ROWCOUNT <> 0
            THROW 50409, 'Instance already exists', @existingStatus;
        SET @newWatermark =  NEXT VALUE FOR dbo.WatermarkSequence;
        SET @instanceKey =  NEXT VALUE FOR dbo.InstanceKeySequence;
        SELECT @studyKey = StudyKey
        FROM   dbo.Study WITH (UPDLOCK)
        WHERE  PartitionKey = @partitionKey
               AND StudyInstanceUid = @studyInstanceUid;
        IF @@ROWCOUNT = 0
            BEGIN TRY
                SET @studyKey =  NEXT VALUE FOR dbo.StudyKeySequence;
                INSERT  INTO dbo.Study (PartitionKey, StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
                VALUES                (@partitionKey, @studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber);
            END TRY
            BEGIN CATCH
                IF ERROR_NUMBER() = 2601
                    BEGIN
                        SELECT @studyKey = StudyKey
                        FROM   dbo.Study WITH (UPDLOCK)
                        WHERE  PartitionKey = @partitionKey
                               AND StudyInstanceUid = @studyInstanceUid;
                        UPDATE dbo.Study
                        SET    PatientId              = @patientId,
                               PatientName            = @patientName,
                               PatientBirthDate       = @patientBirthDate,
                               ReferringPhysicianName = @referringPhysicianName,
                               StudyDate              = @studyDate,
                               StudyDescription       = @studyDescription,
                               AccessionNumber        = @accessionNumber
                        WHERE  PartitionKey = @partitionKey
                               AND StudyKey = @studyKey;
                    END
                ELSE
                    THROW;
            END CATCH
        ELSE
            BEGIN
                UPDATE dbo.Study
                SET    PatientId              = @patientId,
                       PatientName            = @patientName,
                       PatientBirthDate       = @patientBirthDate,
                       ReferringPhysicianName = @referringPhysicianName,
                       StudyDate              = @studyDate,
                       StudyDescription       = @studyDescription,
                       AccessionNumber        = @accessionNumber
                WHERE  PartitionKey = @partitionKey
                       AND StudyKey = @studyKey;
            END
        SELECT @seriesKey = SeriesKey
        FROM   dbo.Series WITH (UPDLOCK)
        WHERE  StudyKey = @studyKey
               AND SeriesInstanceUid = @seriesInstanceUid
               AND PartitionKey = @partitionKey;
        IF @@ROWCOUNT = 0
            BEGIN
                SET @seriesKey =  NEXT VALUE FOR dbo.SeriesKeySequence;
                INSERT  INTO dbo.Series (PartitionKey, StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate, ManufacturerModelName)
                VALUES                 (@partitionKey, @studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate, @manufacturerModelName);
            END
        ELSE
            BEGIN
                UPDATE dbo.Series
                SET    Modality                        = @modality,
                       PerformedProcedureStepStartDate = @performedProcedureStepStartDate,
                       ManufacturerModelName           = @manufacturerModelName
                WHERE  SeriesKey = @seriesKey
                       AND StudyKey = @studyKey
                       AND PartitionKey = @partitionKey;
            END
        INSERT  INTO dbo.Instance (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate, TransferSyntaxUid)
        VALUES                   (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate, @transferSyntaxUid);
        BEGIN TRY
            EXECUTE dbo.IIndexInstanceCoreV9 @partitionKey, @studyKey, @seriesKey, @instanceKey, @newWatermark, @stringExtendedQueryTags, @longExtendedQueryTags, @doubleExtendedQueryTags, @dateTimeExtendedQueryTags, @personNameExtendedQueryTags;
        END TRY
        BEGIN CATCH
            THROW;
        END CATCH
        SELECT @newWatermark,
               @instanceKey;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        THROW;
    END CATCH
END
GO

COMMIT TRANSACTION