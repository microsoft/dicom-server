SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    Instance Table
    Add OriginalWatermark and NewWatermark column.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   (NAME = 'OriginalWatermark' OR NAME = 'NewWatermark')
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    ALTER TABLE dbo.Instance 
    ADD OriginalWatermark BIGINT NULL, NewWatermark BIGINT NULL
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceWithPropertiesV32
--
-- FIRST SCHEMA VERSION
--     32
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level with additional instance properties
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV32 (
    @validStatus        TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON


    SELECT  StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            Watermark,
            TransferSyntaxUid,
            HasFrameMetadata,
            OriginalWatermark,
            NewWatermark
    FROM    dbo.Instance
    WHERE   PartitionKey            = @partitionKey
            AND StudyInstanceUid    = @studyInstanceUid
            AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
            AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
            AND Status              = @validStatus

END
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     BeginUpdateInstance
--
-- DESCRIPTION
--     Updates a DICOM instance NewWatermark
--
-- PARAMETERS
--     @partitionKey
--         * The system identified of the data partition.
--     @watermarkTableType
--         * The SOP instance watermark.
CREATE OR ALTER PROCEDURE dbo.BeginUpdateInstance
    @partitionKey       INT,
    @watermarkTableType dbo.WatermarkTableType READONLY
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION
        
        UPDATE i
        SET NewWatermark = NEXT VALUE FOR dbo.WatermarkSequence
        FROM dbo.Instance i
        JOIN @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
        WHERE Status = 1

    COMMIT TRANSACTION

    SELECT StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        i.Watermark,
        TransferSyntaxUid,
        HasFrameMetadata,
        OriginalWatermark,
        NewWatermark
    FROM dbo.Instance i
    JOIN @watermarkTableType input ON  i.Watermark = input.Watermark AND i.PartitionKey = @partitionKey
    WHERE Status = 1
END
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     EndUpdateInstance
--
-- DESCRIPTION
--     Bulk update all instances in a study, creates new entry in changefeed.
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
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.EndUpdateInstance
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @patientId                          NVARCHAR(64) = NULL,
    @patientName                        NVARCHAR(325) = NULL,
    @patientBirthDate                   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
        DECLARE @updatedInstances AS TABLE
               (PartitionKey INT,
                StudyInstanceUid VARCHAR(64),
                SeriesInstanceUid VARCHAR(64),
                SopInstanceUid VARCHAR(64),
                Watermark BIGINT)

        DELETE FROM @updatedInstances

        UPDATE dbo.Instance
        SET LastStatusUpdatedDate = @currentDate,
            OriginalWatermark = ISNULL(OriginalWatermark, Watermark),
            Watermark = NewWatermark,
            NewWatermark = NULL
        OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.NewWatermark INTO @updatedInstances
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid
            AND Status = 1
            AND NewWatermark IS NOT NULL

        -- Insert into change feed table for update action type
        INSERT INTO dbo.ChangeFeed
        (TimeStamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
        SELECT @currentDate, 2, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
        FROM @updatedInstances

        -- Update existing instance currentWatermark to latest
        UPDATE C
        SET CurrentWatermark = U.Watermark
        FROM dbo.ChangeFeed C
        JOIN @updatedInstances U
        ON C.PartitionKey = U.PartitionKey
            AND C.StudyInstanceUid = U.StudyInstanceUid
            AND C.SeriesInstanceUid = U.SeriesInstanceUid
            AND C.SopInstanceUid = U.SopInstanceUid

        -- Only updating patient information in a study
        UPDATE dbo.Study
        SET PatientId = ISNULL(@patientId, PatientId), 
            PatientName = ISNULL(@patientName, PatientName), 
            PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate)
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid 

        -- The study does not exist. May be deleted
        IF @@ROWCOUNT = 0
            THROW 50404, 'Study does not exist', 1

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION

IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_PartitionKey_Status_StudyInstanceUid_NewWatermark'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyInstanceUid_NewWatermark on dbo.Instance
    (
        PartitionKey,
        Status,
        StudyInstanceUid,
        NewWatermark
    )
    INCLUDE
    (
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        OriginalWatermark
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO
