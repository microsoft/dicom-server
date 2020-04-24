-- NOTE: This script DROPS AND RECREATES all database objects.
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md


/*************************************************************
    Drop existing objects
**************************************************************/

DECLARE @sql nvarchar(max) =''

SELECT @sql = @sql + 'DROP PROCEDURE '+ s.name + '.' + p.name +';'
FROM sys.procedures p
join sys.schemas s
on p.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP TABLE '+ s.name + '.' + t.name +';'
from sys.tables t
join sys.schemas s
on t.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP TYPE '+ s.name + '.' + tt.name +';'
FROM sys.table_types tt
join sys.schemas s
on tt.schema_id = s.schema_id

SELECT @sql = @sql + 'DROP SEQUENCE '+ s.name + '.' + sq.name +';'
FROM sys.sequences sq
join sys.schemas s
on sq.schema_id = s.schema_id

EXEC(@sql)

GO

IF EXISTS ( SELECT *
			FROM sysfulltextcatalogs
			WHERE name = 'Dicom_Catalog' )
	DROP FULLTEXT CATALOG [Dicom_Catalog]
GO

/*************************************************************
    Configure database
**************************************************************/

-- Enable RCSI
IF ((SELECT is_read_committed_snapshot_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON
END

-- Avoid blocking queries when statistics need to be rebuilt
IF ((SELECT is_auto_update_stats_async_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS_ASYNC ON
END

-- Use ANSI behavior for null values
IF ((SELECT is_ansi_nulls_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET ANSI_NULLS ON
END

GO

/*************************************************************
    Schema bootstrap
**************************************************************/

CREATE TABLE dbo.SchemaVersion
(
    Version int PRIMARY KEY,
    Status varchar(10)
)

INSERT INTO dbo.SchemaVersion
VALUES
    (1, 'started')

GO

--
--  STORED PROCEDURE
--      SelectCurrentSchemaVersion
--
--  DESCRIPTION
--      Selects the current completed schema version
--
--  RETURNS
--      The current version as a result set
--
CREATE PROCEDURE dbo.SelectCurrentSchemaVersion
AS
BEGIN
    SET NOCOUNT ON

    SELECT MAX(Version)
    FROM SchemaVersion
    WHERE Status = 'complete'
END
GO

--
--  STORED PROCEDURE
--      UpsertSchemaVersion
--
--  DESCRIPTION
--      Creates or updates a new schema version entry
--
--  PARAMETERS
--      @version
--          * The version number
--      @status
--          * The status of the version
--
CREATE PROCEDURE dbo.UpsertSchemaVersion
    @version int,
    @status varchar(10)
AS
    SET NOCOUNT ON

    IF EXISTS(SELECT *
        FROM dbo.SchemaVersion
        WHERE Version = @version)
    BEGIN
        UPDATE dbo.SchemaVersion
        SET Status = @status
        WHERE Version = @version
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SchemaVersion
            (Version, Status)
        VALUES
            (@version, @status)
    END
GO

/*************************************************************
Full text catalog creation
**************************************************************/
CREATE FULLTEXT CATALOG Dicom_Catalog AS DEFAULT
GO

/*************************************************************
    Instance Table
    Dicom instances with unique Study, Series and Instance Uid
**************************************************************/
CREATE TABLE dbo.Instance (
    --instance keys
    StudyInstanceUid        VARCHAR(64) NOT NULL,
    SeriesInstanceUid       VARCHAR(64) NOT NULL,
    SopInstanceUid          VARCHAR(64) NOT NULL,
    --data consitency columns
    Watermark               BIGINT NOT NULL,
    Status                  TINYINT NOT NULL,
    LastStatusUpdatedDate   DATETIMEOFFSET(7) NOT NULL,
    --audit columns
    CreatedDate             DATETIMEOFFSET(7) NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_Instance on dbo.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)

--Filter indexes
CREATE NONCLUSTERED INDEX IX_Instance_SeriesInstanceUid_SopInstanceUid on dbo.Instance
(
    SeriesInstanceUid,
    SopInstanceUid
)
INCLUDE
(
    StudyInstanceUid,
    Status,
    Watermark
)

CREATE NONCLUSTERED INDEX IX_Instance_SopInstanceUid ON dbo.Instance
(
    SopInstanceUid
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Status,
    Watermark
)

--Cross apply indexes
CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_Status_Watermark on dbo.Instance
(
    StudyInstanceUid,
    Status,
    Watermark DESC
)
INCLUDE
(
    SeriesInstanceUid,
    SopInstanceUid
)

CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status_Watermark on dbo.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Status,
    Watermark DESC
)
INCLUDE
(
    SopInstanceUid
)

/*************************************************************
    Study Table
    Table containing normalized standard Study tags
**************************************************************/
CREATE TABLE dbo.StudyMetadataCore (
    --Key
    Id                          BIGINT NOT NULL, --PK
    --instance keys
    StudyInstanceUid            VARCHAR(64) NOT NULL,
    Version                     INT NOT NULL,
    --patient and study core
    PatientId                   NVARCHAR(64) NOT NULL,
    PatientName                 NVARCHAR(325) NULL,
    ReferringPhysicianName      NVARCHAR(325) NULL,
    StudyDate                   DATE NULL,
    StudyDescription            NVARCHAR(64) NULL,
    AccessionNumber             NVARCHAR(16) NULL,
    PatientNameWords            AS REPLACE(PatientName, '^', ' ')  PERSISTED--FT index,
)

CREATE UNIQUE CLUSTERED INDEX IXC_StudyMetadataCore ON dbo.StudyMetadataCore
(
    Id,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_StudyMetadataCore_PatientId ON dbo.StudyMetadataCore
(
    PatientId
)
INCLUDE
(
    Id,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_StudyMetadataCore_PatientName ON dbo.StudyMetadataCore
(
    PatientName
)
INCLUDE
(
    Id,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_StudyMetadataCore_ReferringPhysicianName ON dbo.StudyMetadataCore
(
    ReferringPhysicianName
)
INCLUDE
(
    Id,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_StudyMetadataCore_StudyDate ON dbo.StudyMetadataCore
(
    StudyDate
)
INCLUDE
(
    Id,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_StudyMetadataCore_StudyDescription ON dbo.StudyMetadataCore
(
    StudyDescription
)
INCLUDE
(
    Id,
    StudyInstanceUid
)

CREATE NONCLUSTERED INDEX IX_StudyMetadataCore_AccessionNumber ON dbo.StudyMetadataCore
(
    AccessionNumber
)
INCLUDE
(
    Id,
    StudyInstanceUid
)

--Full text creation
--unique single column index for FT index
CREATE UNIQUE NONCLUSTERED INDEX IX_StudyMetadataCore_Id ON dbo.StudyMetadataCore
(
    Id
)
INCLUDE
(
	StudyInstanceUid
)

CREATE FULLTEXT INDEX ON StudyMetadataCore(PatientNameWords LANGUAGE 1033)
KEY INDEX IX_StudyMetadataCore_Id
WITH STOPLIST = OFF;


/*************************************************************
    Series Table
    Table containing normalized standard Series tags
**************************************************************/

CREATE TABLE dbo.SeriesMetadataCore (
    --Foreign Key
    StudyId                             BIGINT NOT NULL, --FK
    --instance keys
    SeriesInstanceUid                   VARCHAR(64) NOT NULL,
    Version                             INT NOT NULL,
    --series core
    Modality                            NVARCHAR(16) NULL,
    PerformedProcedureStepStartDate     DATE NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_SeriesMetadataCore ON dbo.SeriesMetadataCore
(
    StudyId,
    SeriesInstanceUid
)

CREATE NONCLUSTERED INDEX IX_SeriesMetadataCore_Modality ON dbo.SeriesMetadataCore
(
    Modality
)
INCLUDE
(
    StudyId,
    SeriesInstanceUid
)

CREATE NONCLUSTERED INDEX IX_SeriesMetadataCore_PerformedProcedureStepStartDate ON dbo.SeriesMetadataCore
(
    PerformedProcedureStepStartDate
)
INCLUDE
(
    StudyId,
    SeriesInstanceUid
)

GO

/*************************************************************
    DeletedInstance Table
    Table containing deleted instances that will be removed after the specified date
**************************************************************/
CREATE TABLE dbo.DeletedInstance
(
    StudyInstanceUid    VARCHAR(64) NOT NULL,
    SeriesInstanceUid   VARCHAR(64) NOT NULL,
    SopInstanceUid      VARCHAR(64) NOT NULL,
    Watermark           BIGINT NOT NULL,
    DeletedDateTime     DATETIMEOFFSET(0) NOT NULL,
    RetryCount          INT NOT NULL,
    CleanupAfter        DATETIMEOFFSET(0) NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_DeletedInstance ON dbo.DeletedInstance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    WaterMark
)

CREATE NONCLUSTERED INDEX IX_DeletedInstance_RetryCount_CleanupAfter ON dbo.DeletedInstance
(
    RetryCount,
    CleanupAfter
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark
)

/*************************************************************
    Sequence for generating unique ids
**************************************************************/

CREATE SEQUENCE dbo.WatermarkSequence
    AS BIGINT
    START WITH 0
    INCREMENT BY 1
    MINVALUE 0
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.MetadataIdSequence
    AS BIGINT
    START WITH 0
    INCREMENT BY 1
    MINVALUE 0
    NO CYCLE
    CACHE 1000000
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstance
--
-- DESCRIPTION
--     Adds a DICOM instance.
--
-- PARAMETERS
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
--
-- RETURN VALUE
--     The watermark (version).
--

CREATE PROCEDURE dbo.AddInstance
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
    @initialStatus                      TINYINT,
    @createDate                         DATETIMEOFFSET(7)
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    DECLARE @existingStatus TINYINT
    DECLARE @metadataId BIGINT
    DECLARE @newVersion BIGINT

    IF EXISTS
        (SELECT *
        FROM dbo.Instance
        WHERE StudyInstanceUid = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid = @sopInstanceUid)
    BEGIN
        THROW 50409, 'Instance already exists', 1;
    END

    -- The instance does not exist, insert it.
    SET @newVersion = NEXT VALUE FOR dbo.WatermarkSequence

    INSERT INTO dbo.Instance
        (StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newVersion, @initialStatus, @createDate, @createDate)

    -- Update the study metadata if needed.
    SELECT @metadataId = Id
    FROM dbo.StudyMetadataCore
    WHERE StudyInstanceUid = @studyInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @metadataId = NEXT VALUE FOR dbo.MetadataIdSequence

        INSERT INTO dbo.StudyMetadataCore
            (Id, StudyInstanceUid, Version, PatientId, PatientName, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@metadataId, @studyInstanceUid, 0, @patientId, @patientName, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    --ELSE BEGIN
        -- TODO: handle the versioning
    --END

    IF NOT EXISTS (SELECT * FROM dbo.SeriesMetadataCore WHERE StudyId = @metadataId AND SeriesInstanceUid = @seriesInstanceUid)
    BEGIN
        INSERT INTO dbo.SeriesMetadataCore
            (StudyId, SeriesInstanceUid, Version, Modality, PerformedProcedureStepStartDate)
        VALUES
            (@metadataId, @seriesInstanceUid, 0, @modality, @performedProcedureStepStartDate)
    END
    --ELSE BEGIN
        -- TODO: handle the versioning
    --END

    SELECT @newVersion

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for updating an instance status.
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateInstanceStatus
--
-- DESCRIPTION
--     Updates a DICOM instance status.
--
-- PARAMETERS
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
--
-- RETURN VALUE
--     None
--
CREATE PROCEDURE dbo.UpdateInstanceStatus
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @status             TINYINT,
    @updateDate         DATETIMEOFFSET(7)
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @updateDate
    WHERE StudyInstanceUid = @studyInstanceUid
    AND SeriesInstanceUid = @seriesInstanceUid
    AND SopInstanceUid = @sopInstanceUid
    AND Watermark = @watermark

    IF @@ROWCOUNT = 0
    BEGIN
        -- The instance does not exist. Perhaps it was deleted?
        THROW 50404, 'Instance does not exist', 1;
    END

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstance
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE PROCEDURE dbo.GetInstance (
    @invalidStatus      TINYINT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    IF ( @seriesInstanceUid IS NOT NULL AND @sopInstanceUid IS NOT NULL )
    BEGIN
        SELECT  StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                Watermark
        FROM    dbo.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND SeriesInstanceUid   = @seriesInstanceUid
                AND SopInstanceUid      = @sopInstanceUid
                AND Status              <> @invalidStatus
    END
    ELSE IF ( @seriesInstanceUid IS NOT NULL )
    BEGIN
        SELECT  StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                Watermark
        FROM    dbo.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND SeriesInstanceUid   = @seriesInstanceUid
                AND Status              <> @invalidStatus
    END
    ELSE
    BEGIN
        SELECT  StudyInstanceUid,
                SeriesInstanceUid,
                SopInstanceUid,
                Watermark
        FROM    dbo.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND Status              <> @invalidStatus
    END
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteInstance
--
-- DESCRIPTION
--     Removes the specified instance(s) and places them in the DeletedInstance table for later removal
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE PROCEDURE dbo.DeleteInstance (
    @deletedDate        DATETIMEOFFSET(0),
    @cleanupAfter       DATETIMEOFFSET(0),
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE

    BEGIN TRANSACTION

    DECLARE @studyId bigint

    -- Get the study PK
    SELECT  @studyId = ID
    FROM    dbo.StudyMetadataCore
    WHERE   StudyInstanceUid = @studyInstanceUid;

    -- Delete the instance and insert the details into FileCleanup
    DELETE  dbo.Instance
        OUTPUT deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Watermark, @deletedDate, 0, @cleanupAfter
        INTO dbo.DeletedInstance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    IF (@@ROWCOUNT = 0)
    BEGIN
        THROW 50404, 'Instance not found', 1;
    END

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance
                    WHERE   StudyInstanceUid = @studyInstanceUid
                    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid))
    BEGIN
        DELETE
        FROM    dbo.SeriesMetadataCore
        WHERE   StudyId = @studyId
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    END

    -- If we've removing the series, see if it's the last for a study and if so, remove the study
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.SeriesMetadataCore
                    WHERE   StudyId = @studyId)
    BEGIN
        DELETE
        FROM    dbo.StudyMetadataCore
        WHERE   Id = @studyId
    END

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstance
--
-- DESCRIPTION
--     Retrieves deleted instances where the cleanupAfter is less than the date passed in and the retry count hasn't been exceeded
--
-- PARAMETERS
--     @cleanupAfter
--         * The date time for the cutoff on retrieving deleted instances
--     @count
--         * The number of entries to return
--     @maxRetries
--         * The maximum number of times to retry a cleanup
/***************************************************************************************/
CREATE PROCEDURE dbo.RetrieveDeletedInstance
    @cleanupAfter   DATETIMEOFFSET(0),
    @count          INT,
    @maxRetries     INT
AS
    SET NOCOUNT ON

    SELECT  TOP (@count) StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM    dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < @cleanupAfter
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteDeletedInstance
--
-- DESCRIPTION
--     Removes a deleted instance from the DeletedInstance table
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
/***************************************************************************************/
CREATE PROCEDURE dbo.DeleteDeletedInstance(
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT
)
AS
    SET NOCOUNT ON

    DELETE
    FROM    dbo.DeletedInstance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = @seriesInstanceUid
    AND     SopInstanceUid = @sopInstanceUid
    AND     Watermark = @watermark
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     IncrementDeletedInstanceRetry
--
-- DESCRIPTION
--     Increments the retryCount of and retryAfter of a deleted instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
--     @cleanupAfter
--         * The next date time to attempt cleanup
--
-- RETURN VALUE
--     The retry count.
--
/***************************************************************************************/
CREATE PROCEDURE dbo.IncrementDeletedInstanceRetry(
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT,
    @cleanupAfter       DATETIMEOFFSET(0)
)
AS
    SET NOCOUNT ON

    DECLARE @retryCount INT

    UPDATE  dbo.DeletedInstance
    SET     @retryCount = RetryCount = RetryCount + 1,
            CleanupAfter = @cleanupAfter
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = @seriesInstanceUid
    AND     SopInstanceUid = @sopInstanceUid
    AND     Watermark = @watermark

    SELECT @retryCount
GO
