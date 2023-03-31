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

/**************************************************************/
--
-- STORED PROCEDURE
--     GetInstancesByStudyAndWatermark
--
-- DESCRIPTION
--     Get instances by given minimum watermark in a study.
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @partitionKey
--         * The system identified of the data partition.
--     @studyInstanceUid
--         * The study instance UID.
--     @maxWatermark
--         * The optional maxWatermark.
-- RETURN VALUE
--     The instance identifiers.
------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetInstancesByStudyAndWatermark
    @batchSize INT,
    @partitionKey INT,
    @studyInstanceUid VARCHAR(64),
    @maxWatermark BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON
    SELECT TOP (@batchSize)
        StudyInstanceUid,
        SeriesInstanceUid,
        SopInstanceUid,
        Watermark,
        OriginalWatermark,
        NewWatermark
    FROM dbo.Instance
    WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND Watermark >= ISNULL(@maxWatermark, Watermark)
        AND Status = 1
    ORDER BY Watermark ASC
END
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceNewWatermark
--
-- DESCRIPTION
--     Updates a DICOM instance NewWatermark
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
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateInstanceNewWatermark
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION
        
        DECLARE @newWatermark BIGINT

        SET @newWatermark = NEXT VALUE FOR dbo.WatermarkSequence

        UPDATE dbo.Instance
        SET NewWatermark = @newWatermark
        WHERE PartitionKey = @partitionKey
        AND StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid
        AND Status = 1

        -- The instance does not exist.
        IF @@ROWCOUNT = 0
            THROW 50404, 'Instance does not exist', 1

    COMMIT TRANSACTION
END
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     BulkUpdateStudyInstance
--
-- DESCRIPTION
--     Bulk update all instances in a study, and update extendedquerytag with new watermark.
--
-- PARAMETERS
--     @batchSize
--         * The desired number of instances per batch. Actual number may be smaller.
--     @partitionKey
--         * The partition key.
--     @studyInstanceUid
--         * The study instance UID.
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
--     @patientBirthDate
--         * The patient's birth date.
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.BulkUpdateStudyInstance
    @batchSize                          INT,
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @patientId                          NVARCHAR(64) = NULL,
    @patientName                        NVARCHAR(325) = NULL,
    @referringPhysicianName             NVARCHAR(325) = NULL,
    @studyDate                          DATE = NULL,
    @studyDescription                   NVARCHAR(64) = NULL,
    @accessionNumber                    NVARCHAR(64) = NULL,
    @patientBirthDate                   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @rowsUpdated INT = 0
    DECLARE @imageResourceType AS TINYINT = 0
    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
    DECLARE @updatedInstances AS TABLE
           (PartitionKey INT,
            StudyInstanceUid VARCHAR(64),
            SeriesInstanceUid VARCHAR(64),
            SopInstanceUid VARCHAR(64),
            StudyKey BIGINT,
            SeriesKey BIGINT,
            InstanceKey BIGINT,
            Watermark BIGINT)

    DECLARE @totalCount INT = (SELECT COUNT(*) FROM dbo.Instance WHERE PartitionKey = @partitionKey AND StudyInstanceUid = @studyInstanceUid AND Status = 1 AND NewWatermark IS NOT NULL) 

    WHILE (@rowsUpdated < @totalCount)
    BEGIN
        
        DELETE FROM @updatedInstances

        UPDATE TOP (@batchSize) dbo.Instance
        SET LastStatusUpdatedDate = @currentDate, OriginalWatermark = Watermark, Watermark = NewWatermark, NewWatermark = NULL
        OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.StudyKey, deleted.SeriesKey, deleted.InstanceKey, deleted.NewWatermark  INTO @updatedInstances
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid
            AND Status = 1
            AND NewWatermark IS NOT NULL

        SET @rowsUpdated = @rowsUpdated + @@ROWCOUNT;

        UPDATE EQT
        SET Watermark = U.Watermark
        FROM ExtendedQueryTagString EQT
        JOIN @updatedInstances U 
        ON EQT.SopInstanceKey1 = U.StudyKey
        AND EQT.SopInstanceKey2 = U.SeriesKey
        AND EQT.SopInstanceKey3 = U.InstanceKey
        AND EQT.PartitionKey = @partitionKey
        AND EQT.ResourceType = @imageResourceType

        UPDATE EQT
        SET Watermark = U.Watermark
        FROM ExtendedQueryTagLong EQT
        JOIN @updatedInstances U 
        ON EQT.SopInstanceKey1 = U.StudyKey
        AND EQT.SopInstanceKey2 = U.SeriesKey
        AND EQT.SopInstanceKey3 = U.InstanceKey
        AND EQT.PartitionKey = @partitionKey
        AND EQT.ResourceType = @imageResourceType

        UPDATE EQT
        SET Watermark = U.Watermark
        FROM ExtendedQueryTagDouble EQT
        JOIN @updatedInstances U 
        ON EQT.SopInstanceKey1 = U.StudyKey
        AND EQT.SopInstanceKey2 = U.SeriesKey
        AND EQT.SopInstanceKey3 = U.InstanceKey
        AND EQT.PartitionKey = @partitionKey
        AND EQT.ResourceType = @imageResourceType

        UPDATE EQT
        SET Watermark = U.Watermark
        FROM ExtendedQueryTagDateTime EQT
        JOIN @updatedInstances U 
        ON EQT.SopInstanceKey1 = U.StudyKey
        AND EQT.SopInstanceKey2 = U.SeriesKey
        AND EQT.SopInstanceKey3 = U.InstanceKey
        AND EQT.PartitionKey = @partitionKey
        AND EQT.ResourceType = @imageResourceType

        UPDATE EQT
        SET Watermark = U.Watermark
        FROM ExtendedQueryTagPersonName EQT
        JOIN @updatedInstances U 
        ON EQT.SopInstanceKey1 = U.StudyKey
        AND EQT.SopInstanceKey2 = U.SeriesKey
        AND EQT.SopInstanceKey3 = U.InstanceKey
        AND EQT.PartitionKey = @partitionKey
        AND EQT.ResourceType = @imageResourceType

        -- Insert into change feed table for update action type
        INSERT INTO dbo.ChangeFeed
        (TimeStamp, Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark, CurrentWatermark)
        SELECT @currentDate, 2, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Watermark
        FROM @updatedInstances
    END

    UPDATE dbo.Study
    SET PatientId = ISNULL(@patientId, PatientId), 
        PatientName = ISNULL(@patientName, PatientName), 
        PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate), 
        ReferringPhysicianName = ISNULL(@referringPhysicianName, ReferringPhysicianName), 
        StudyDate = ISNULL(@studyDate, StudyDate), 
        StudyDescription = ISNULL(@studyDescription, StudyDescription), 
        AccessionNumber = ISNULL(@accessionNumber, AccessionNumber)
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
    WHERE   NAME = 'IX_Instance_PartitionKey_Status_StudyInstanceUid_Watermark'
        AND Object_id = OBJECT_ID('dbo.Instance')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_Status_StudyInstanceUid_Watermark on dbo.Instance
    (
        PartitionKey,
        Status,
        StudyInstanceUid,
        Watermark
    )
    INCLUDE
    (
        SeriesInstanceUid,
        SopInstanceUid,
        OriginalWatermark,
        NewWatermark  
    )
    WITH (DATA_COMPRESSION = PAGE)
END
GO

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
