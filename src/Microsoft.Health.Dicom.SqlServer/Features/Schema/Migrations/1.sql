-- NOTE: This script DROPS AND RECREATES all database objects.
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md

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
Full text catalog creation
**************************************************************/
CREATE FULLTEXT CATALOG Dicom_Catalog WITH ACCENT_SENSITIVITY = OFF AS DEFAULT
GO

/*************************************************************
    Instance Table
    Dicom instances with unique Study, Series and Instance Uid
**************************************************************/
CREATE TABLE dbo.Instance (
    InstanceKey             BIGINT                     NOT NULL, --PK
    SeriesKey               BIGINT                     NOT NULL, --FK
    -- StudyKey needed to join directly from Study table to find a instance
    StudyKey                BIGINT                     NOT NULL, --FK
    --instance keys used in WADO
    StudyInstanceUid        VARCHAR(64)                NOT NULL,
    SeriesInstanceUid       VARCHAR(64)                NOT NULL,
    SopInstanceUid          VARCHAR(64)                NOT NULL,
    --data consitency columns
    Watermark               BIGINT                     NOT NULL,
    Status                  TINYINT                    NOT NULL,
    LastStatusUpdatedDate   DATETIME2(7)               NOT NULL,
    --audit columns
    CreatedDate             DATETIME2(7)               NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Instance on dbo.Instance
(
    SeriesKey,
    InstanceKey
)

--Filter indexes
CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid on dbo.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
INCLUDE
(
    Status,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_Status on dbo.Instance
(
    StudyInstanceUid,
    Status
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status on dbo.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Status
)
INCLUDE
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_SopInstanceUid_Status on dbo.Instance
(
    SopInstanceUid,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_Watermark on dbo.Instance
(
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

--Cross apply indexes
CREATE NONCLUSTERED INDEX IX_Instance_SeriesKey_Status on dbo.Instance
(
    SeriesKey,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_StudyKey_Status on dbo.Instance
(
    StudyKey,
    Status
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

/*************************************************************
    Study Table
    Table containing normalized standard Study tags
**************************************************************/
CREATE TABLE dbo.Study (
    StudyKey                    BIGINT                            NOT NULL, --PK
    StudyInstanceUid            VARCHAR(64)                       NOT NULL,
    PatientId                   NVARCHAR(64)                      NOT NULL,
    PatientName                 NVARCHAR(200)                     COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    ReferringPhysicianName      NVARCHAR(200)                     COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    StudyDate                   DATE                              NULL,
    StudyDescription            NVARCHAR(64)                      NULL,
    AccessionNumber             NVARCHAR(16)                      NULL,
    PatientNameWords            AS REPLACE(REPLACE(PatientName, '^', ' '), '=', ' ') PERSISTED,
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Study ON dbo.Study
(
    StudyKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyInstanceUid ON dbo.Study
(
    StudyInstanceUid
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_PatientId ON dbo.Study
(
    PatientId
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_PatientName ON dbo.Study
(
    PatientName
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_ReferringPhysicianName ON dbo.Study
(
    ReferringPhysicianName
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_StudyDate ON dbo.Study
(
    StudyDate
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_StudyDescription ON dbo.Study
(
    StudyDescription
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_AccessionNumber ON dbo.Study
(
    AccessionNumber
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE FULLTEXT INDEX ON Study(PatientNameWords LANGUAGE 1033)
KEY INDEX IXC_Study
WITH STOPLIST = OFF;


/*************************************************************
    Series Table
    Table containing normalized standard Series tags
**************************************************************/

CREATE TABLE dbo.Series (
    SeriesKey                           BIGINT                     NOT NULL, --PK
    StudyKey                            BIGINT                     NOT NULL, --FK
    SeriesInstanceUid                   VARCHAR(64)                NOT NULL,
    Modality                            NVARCHAR(16)               NULL,
    PerformedProcedureStepStartDate     DATE                       NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Series ON dbo.Series
(
    StudyKey,
    SeriesKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_Series_SeriesKey ON dbo.Series
(
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE NONCLUSTERED INDEX IX_Series_SeriesInstanceUid ON dbo.Series
(
    SeriesInstanceUid
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Series_Modality ON dbo.Series
(
    Modality
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Series_PerformedProcedureStepStartDate ON dbo.Series
(
    PerformedProcedureStepStartDate
)
INCLUDE
(
    StudyKey,
    SeriesKey
)
WITH (DATA_COMPRESSION = PAGE)

GO

/*************************************************************
    DeletedInstance Table
    Table containing deleted instances that will be removed after the specified date
**************************************************************/
CREATE TABLE dbo.DeletedInstance
(
    StudyInstanceUid    VARCHAR(64)       NOT NULL,
    SeriesInstanceUid   VARCHAR(64)       NOT NULL,
    SopInstanceUid      VARCHAR(64)       NOT NULL,
    Watermark           BIGINT            NOT NULL,
    DeletedDateTime     DATETIMEOFFSET(0) NOT NULL,
    RetryCount          INT               NOT NULL,
    CleanupAfter        DATETIMEOFFSET(0) NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

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
WITH (DATA_COMPRESSION = PAGE)

/*************************************************************
    Changes Table
    Stores Add/Delete immutable actions
    Only CurrentWatermark is updated to reflect the current state.
    Current Instance State
    CurrentWatermark = null,               Current State = Deleted
    CurrentWatermark = OriginalWatermark,  Current State = Created
    CurrentWatermark <> OriginalWatermark, Current State = Replaced
**************************************************************/
CREATE TABLE dbo.ChangeFeed (
    Sequence                BIGINT IDENTITY(1,1) NOT NULL,
    Timestamp               DATETIMEOFFSET(7)    NOT NULL,
    Action                  TINYINT              NOT NULL,
    StudyInstanceUid        VARCHAR(64)          NOT NULL,
    SeriesInstanceUid       VARCHAR(64)          NOT NULL,
    SopInstanceUid          VARCHAR(64)          NOT NULL,
    OriginalWatermark       BIGINT               NOT NULL,
    CurrentWatermark        BIGINT               NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_ChangeFeed ON dbo.ChangeFeed
(
    Sequence
)

CREATE NONCLUSTERED INDEX IX_ChangeFeed_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid ON dbo.ChangeFeed
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)

/*************************************************************
    Sequence for generating sequential unique ids
**************************************************************/

CREATE SEQUENCE dbo.WatermarkSequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.StudyKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.SeriesKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.InstanceKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
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
------------------------------------------------------------------------

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
    @initialStatus                      TINYINT
AS
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
    WHERE StudyInstanceUid = @studyInstanceUid
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
    WHERE StudyInstanceUid = @studyInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

        INSERT INTO dbo.Study
            (StudyKey, StudyInstanceUid, PatientId, PatientName, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
        VALUES
            (@studyKey, @studyInstanceUid, @patientId, @patientName, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Study
        SET PatientId = @patientId, PatientName = @patientName, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
        WHERE StudyKey = @studyKey
    END

    -- Insert Series
    SELECT @seriesKey = SeriesKey
    FROM dbo.Series WITH(UPDLOCK)
    WHERE StudyKey = @studyKey
    AND SeriesInstanceUid = @seriesInstanceUid

    IF @@ROWCOUNT = 0
    BEGIN
        SET @seriesKey = NEXT VALUE FOR dbo.SeriesKeySequence

        INSERT INTO dbo.Series
            (StudyKey, SeriesKey, SeriesInstanceUid, Modality, PerformedProcedureStepStartDate)
        VALUES
            (@studyKey, @seriesKey, @seriesInstanceUid, @modality, @performedProcedureStepStartDate)
    END
    ELSE
    BEGIN
        -- Latest wins
        UPDATE dbo.Series
        SET Modality = @modality, PerformedProcedureStepStartDate = @performedProcedureStepStartDate
        WHERE SeriesKey = @seriesKey
        AND StudyKey = @studyKey
    END

    -- Insert Instance
    INSERT INTO dbo.Instance
        (StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate)
    VALUES
        (@studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @newWatermark, @initialStatus, @currentDate, @currentDate)

    SELECT @newWatermark

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
    @status             TINYINT
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()

    UPDATE dbo.Instance
    SET Status = @status, LastStatusUpdatedDate = @currentDate
    WHERE StudyInstanceUid = @studyInstanceUid
    AND SeriesInstanceUid = @seriesInstanceUid
    AND SopInstanceUid = @sopInstanceUid
    AND Watermark = @watermark

    IF @@ROWCOUNT = 0
        -- The instance does not exist. Perhaps it was deleted?
        THROW 50404, 'Instance does not exist', 1;

    -- Insert to change feed.
    -- Currently this procedure is used only updating the status to created
    -- If that changes an if condition is needed.
    INSERT INTO dbo.ChangeFeed
        (Timestamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    VALUES
        (@currentDate, 0, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @watermark)

    -- Update existing instance currentWatermark to latest
    UPDATE dbo.ChangeFeed
    SET CurrentWatermark      = @watermark
    WHERE StudyInstanceUid    = @studyInstanceUid
        AND SeriesInstanceUid = @seriesInstanceUid
        AND SopInstanceUid    = @sopInstanceUid

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
    @validStatus        TINYINT,
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
                Watermark
        FROM    dbo.Instance
        WHERE   StudyInstanceUid        = @studyInstanceUid
                AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
                AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
                AND Status              = @validStatus

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
--     @cleanupAfter
--         * The date time offset that the instance can be cleaned up.
--     @createdStatus
--         * Status value representing the created state.
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE PROCEDURE dbo.DeleteInstance (
    @cleanupAfter       DATETIMEOFFSET(0),
    @createdStatus      TINYINT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = null,
    @sopInstanceUid     VARCHAR(64) = null
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRANSACTION

    DECLARE @deletedInstances AS TABLE
        (StudyInstanceUid VARCHAR(64),
         SeriesInstanceUid VARCHAR(64),
         SopInstanceUid VARCHAR(64),
         Status TINYINT,
         Watermark BIGINT)

    DECLARE @studyKey BIGINT
    DECLARE @deletedDate DATETIME2 = SYSUTCDATETIME()

    -- Get the study PK
    SELECT  @studyKey = StudyKey
    FROM    dbo.Instance
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    -- Delete the instance and insert the details into DeletedInstance and ChangeFeed
    DELETE  dbo.Instance
        OUTPUT deleted.StudyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, deleted.Status, deleted.Watermark
        INTO @deletedInstances
    WHERE   StudyInstanceUid = @studyInstanceUid
    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    AND     SopInstanceUid = ISNULL(@sopInstanceUid, SopInstanceUid)

    IF (@@ROWCOUNT = 0)
        THROW 50404, 'Instance not found', 1;

    INSERT INTO dbo.DeletedInstance
    (StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, DeletedDateTime, RetryCount, CleanupAfter)
    SELECT StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, @deletedDate, 0 , @cleanupAfter
    FROM @deletedInstances

    INSERT INTO dbo.ChangeFeed
    (TimeStamp, Action, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT @deletedDate, 1, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM @deletedInstances
    WHERE Status = @createdStatus

    UPDATE cf
    SET cf.CurrentWatermark = NULL
    FROM dbo.ChangeFeed cf
    JOIN @deletedInstances d
    ON cf.StudyInstanceUid = d.StudyInstanceUid
        AND cf.SeriesInstanceUid = d.SeriesInstanceUid
        AND cf.SopInstanceUid = d.SopInstanceUid

    -- If this is the last instance for a series, remove the series
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Instance WITH(HOLDLOCK, UPDLOCK)
                    WHERE   StudyKey = @studyKey
                    AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid))
    BEGIN
        DELETE
        FROM    dbo.Series
        WHERE   Studykey = @studyKey
        AND     SeriesInstanceUid = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
    END

    -- If we've removing the series, see if it's the last for a study and if so, remove the study
    IF NOT EXISTS ( SELECT  *
                    FROM    dbo.Series WITH(HOLDLOCK, UPDLOCK)
                    WHERE   Studykey = @studyKey)
    BEGIN
        DELETE
        FROM    dbo.Study
        WHERE   Studykey = @studyKey
    END

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     RetrieveDeletedInstance
--
-- DESCRIPTION
--     Retrieves deleted instances where the cleanupAfter is less than the current date in and the retry count hasn't been exceeded
--
-- PARAMETERS
--     @count
--         * The number of entries to return
--     @maxRetries
--         * The maximum number of times to retry a cleanup
/***************************************************************************************/
CREATE PROCEDURE dbo.RetrieveDeletedInstance
    @count          INT,
    @maxRetries     INT
AS
    SET NOCOUNT ON

    SELECT  TOP (@count) StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark
    FROM    dbo.DeletedInstance WITH (UPDLOCK, READPAST)
    WHERE   RetryCount <= @maxRetries
    AND     CleanupAfter < SYSUTCDATETIME()
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

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeed
--
-- DESCRIPTION
--     Gets a stream of dicom changes (instance adds and deletes)
--
-- PARAMETERS
--     @limit
--         * Max rows to return
--     @offet
--         * Rows to skip
/***************************************************************************************/
CREATE PROCEDURE dbo.GetChangeFeed (
    @limit      INT,
    @offset     BIGINT)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  Sequence,
            Timestamp,
            Action,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed
    WHERE   Sequence BETWEEN @offset+1 AND @offset+@limit
    ORDER BY Sequence
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetChangeFeedLatest
--
-- DESCRIPTION
--     Gets the latest dicom change
/***************************************************************************************/
CREATE PROCEDURE dbo.GetChangeFeedLatest
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  TOP(1)
            Sequence,
            Timestamp,
            Action,
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            OriginalWatermark,
            CurrentWatermark
    FROM    dbo.ChangeFeed
    ORDER BY Sequence DESC
END
GO
