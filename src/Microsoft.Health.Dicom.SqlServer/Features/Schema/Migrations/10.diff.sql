SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    Instance Table
    Add TransferSyntaxUid column.
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   NAME = 'TransferSyntaxUid'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    ALTER TABLE dbo.Instance ADD TransferSyntaxUid VARCHAR(64) NULL
END
GO

/*************************************************************
    New GetInstanceWithProperties Stored procedure
**************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithProperties (
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
            TransferSyntaxUid
    FROM    dbo.Instance
    WHERE   PartitionKey            = @partitionKey
            AND StudyInstanceUid    = @studyInstanceUid
            AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
            AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
            AND Status              = @validStatus

END
GO

/*************************************************************
    AddInstance Stored procedure
    Add new TransferSyntaxUid param with default
**************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddInstanceV6
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @seriesInstanceUid                  VARCHAR(64),
    @sopInstanceUid                     VARCHAR(64),
    @patientId                          NVARCHAR(64),
    @patientName                        NVARCHAR(325) = NULL,
    @referringPhysicianName             NVARCHAR(325) = NULL,
    @studyDate                          DATE = NULL,
    @studyDescription                   NVARCHAR(64) = NULL,
    @accessionNumber                    NVARCHAR(64) = NULL,
    @modality                           NVARCHAR(16) = NULL,
    @performedProcedureStepStartDate    DATE = NULL,
    @patientBirthDate                   DATE = NULL,
    @manufacturerModelName              NVARCHAR(64) = NULL,
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_2 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY,
    @initialStatus                      TINYINT,
    @transferSyntaxUid                  VARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @existingStatus TINYINT
    DECLARE @newWatermark BIGINT
    DECLARE @studyKey BIGINT
    DECLARE @seriesKey BIGINT
    DECLARE @instanceKey BIGINT

    SELECT @existingStatus = Status
    FROM dbo.Instance
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid

    IF @@ROWCOUNT <> 0
        -- The instance already exists. Set the state = @existingStatus to indicate what state it is in.
        THROW 50409, 'Instance already exists', @existingStatus;

    -- The instance does not exist, insert it.
    SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence
    SET @instanceKey = NEXT VALUE FOR dbo.InstanceKeySequence

    -- Insert Study
    SELECT @studyKey = StudyKey
    FROM dbo.Study WITH(UPDLOCK)
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

        INSERT INTO dbo.Study
            (PartitionKey, StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@partitionKey, @studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Study
        SET PatientId = @patientId, PatientName = @patientName, PatientBirthDate = @patientBirthDate, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
        WHERE StudyKey = @studyKey
    END

    -- Insert Series
    SELECT @seriesKey = SeriesKey
    FROM dbo.Series WITH(UPDLOCK)
    WHERE StudyKey = @studyKey
    AND SeriesInstanceUid = @seriesInstanceUid
    AND PartitionKey = @partitionKey

    IF @@ROWCOUNT = 0
    BEGIN
        SET @seriesKey = NEXT VALUE FOR dbo.SeriesKeySequence

        INSERT INTO dbo.Series
            (PartitionKey, StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate, ManufacturerModelName)
        VALUES
            (@partitionKey, @studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate, @manufacturerModelName)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Series
        SET Modality = @modality, PerformedProcedureStepStartDate = @performedProcedureStepStartDate, ManufacturerModelName = @manufacturerModelName
        WHERE SeriesKey = @seriesKey
        AND StudyKey = @studyKey
        AND PartitionKey = @partitionKey
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate, TransferSyntaxUid)
    VALUES
        (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate, @transferSyntaxUid)

    BEGIN TRY

        EXEC dbo.IIndexInstanceCoreV9
            @partitionKey,
            @studyKey,
            @seriesKey,
            @instanceKey,
            @newWatermark,
            @stringExtendedQueryTags,
            @longExtendedQueryTags,
            @doubleExtendedQueryTags,
            @dateTimeExtendedQueryTags,
            @personNameExtendedQueryTags

    END TRY
    BEGIN CATCH

        THROW

    END CATCH

    SELECT @newWatermark

    COMMIT TRANSACTION
END
GO

COMMIT TRANSACTION

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_StudyInstanceUid_Status_PartitionKey'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_Status_PartitionKey on dbo.Instance
    (
        StudyInstanceUid,
        Status,
        PartitionKey    
    )
    INCLUDE
    (
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        TransferSyntaxUid
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING = ON, ONLINE = ON)
END

IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_StudyInstanceUid_Status_PartitionKey'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_Status_PartitionKey on dbo.Instance
    (
        StudyInstanceUid,
        Status,
        PartitionKey    
    )
    INCLUDE
    (
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        TransferSyntaxUid
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING = ON, ONLINE = ON)
END


IF EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status_PartitionKey'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status_PartitionKey on dbo.Instance
    (
        StudyInstanceUid,
        SeriesInstanceUid,
        Status,
        PartitionKey    
    )
    INCLUDE
    (
        SopInstanceUid,
        Watermark,
        TransferSyntaxUid
    )
    WITH (DATA_COMPRESSION = PAGE, DROP_EXISTING = ON, ONLINE = ON)
END
