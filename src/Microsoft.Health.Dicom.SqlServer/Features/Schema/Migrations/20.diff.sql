/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/

SET XACT_ABORT ON

/****************************************************************************************
Stored Procedures
******************************************************************************************/
BEGIN TRANSACTION
GO

/*************************************************************
    Stored procedure for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstanceV6
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
--     The watermark (version).
------------------------------------------------------------------------
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

    -- We turn off XACT_ABORT so that we can rollback and retry the INSERT/UPDATE into the study table on failure
    SET XACT_ABORT OFF

    -- The transaction is wrapped in a try...catch block in case the INSERT into the study table fails
    BEGIN TRY

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
            -- If we fail to INSERT, we instead must UPDATE the newly added value
            SELECT @studyKey = StudyKey
            FROM dbo.Study WITH(UPDLOCK)
            WHERE PartitionKey = @partitionKey
                AND StudyInstanceUid = @studyInstanceUid

            IF @@ROWCOUNT = 0
            BEGIN TRY

                SET @studyKey = NEXT VALUE FOR dbo.StudyKeySequence

                INSERT INTO dbo.Study
                    (PartitionKey, StudyKey, StudyInstanceUid, PatientId, PatientName, PatientBirthDate, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumber)
                VALUES
                    (@partitionKey, @studyKey, @studyInstanceUid, @patientId, @patientName, @patientBirthDate, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)

            END TRY
            BEGIN CATCH

                 -- While we could obtain a HOLDLOCK on the table, we optimistically obtain an UPDLOCK instead to avoid the range lock on the study table
                IF ERROR_NUMBER() = 2601
                BEGIN

                    SELECT @studyKey = StudyKey
                    FROM dbo.Study WITH(UPDLOCK)
                    WHERE PartitionKey = @partitionKey
                        AND StudyInstanceUid = @studyInstanceUid

                    -- Latest wins
                    UPDATE dbo.Study
                    SET PatientId = @patientId, PatientName = @patientName, PatientBirthDate = @patientBirthDate, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
                    WHERE StudyKey = @studyKey

                END
                ELSE
                    THROW

            END CATCH
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

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW

    END CATCH
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    DeleteExtendedQueryTagV16
--
-- DESCRIPTION
--    Delete the specified extended query tag index and its associated metadata
--
-- PARAMETERS
--     @tagPath
--         * The extended query tag path
--     @dataType
--         * the data type of extended query tag. 0 -- String, 1 -- Long, 2 -- Double, 3 -- DateTime, 4 -- PersonName
--     @batchSize
--         * the size of each deletion batch
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteExtendedQueryTagV16
    @tagPath VARCHAR(64),
    @dataType TINYINT,
    @batchSize INT = 1000
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    DECLARE @deletedRows INT

    BEGIN TRANSACTION

        DECLARE @tagKey INT
        DECLARE @imageResourceType TINYINT = 0

        SELECT @tagKey = TagKey
        FROM dbo.ExtendedQueryTag WITH(XLOCK)
        WHERE dbo.ExtendedQueryTag.TagPath = @tagPath

        -- Check existence
        IF @@ROWCOUNT = 0
            THROW 50404, 'extended query tag not found', 1

        -- Update status to Deleting
        UPDATE dbo.ExtendedQueryTag
        SET TagStatus = 2
        WHERE dbo.ExtendedQueryTag.TagKey = @tagKey

    COMMIT TRANSACTION

    -- Delete index data
    SET @deletedRows = @batchSize
    WHILE (@deletedRows = @batchSize)
    BEGIN

        EXEC dbo.ISleepIfBusy

        BEGIN TRANSACTION

            IF @dataType = 0
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagString WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE IF @dataType = 1
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagLong WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE IF @dataType = 2
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagDouble WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE IF @dataType = 3
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagDateTime WHERE TagKey = @tagKey AND ResourceType = @imageResourceType
            ELSE
                DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagPersonName WHERE TagKey = @tagKey AND ResourceType = @imageResourceType

            SET @deletedRows = @@ROWCOUNT

        COMMIT TRANSACTION
        CHECKPOINT

    END

    -- Delete errors
    SET @deletedRows = @batchSize
    WHILE (@deletedRows = @batchSize)
    BEGIN

        EXEC dbo.ISleepIfBusy

        BEGIN TRANSACTION

            DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagError
            WHERE TagKey = @tagKey

            SET @deletedRows = @@ROWCOUNT

        COMMIT TRANSACTION
        CHECKPOINT

    END

    -- Delete tag
    DELETE FROM dbo.ExtendedQueryTag
    WHERE TagKey = @tagKey
END
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Sleep If Busy
--
-- DESCRIPTION
--    This stored procedure checks if SQL Azure/Server is busy and sleeps if this
--    is the case. This stored procedure is not very useful on SQL Server, because
--    service account normally does not have a 'VIEW SERVER STATE' permission.
--    Note: In on-prem case this sproc does not do anything.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.ISleepIfBusy
AS
BEGIN
    DECLARE @throttleCount INT
    DECLARE @activeRequestCount INT
    DECLARE @sleepersCount INT
    DECLARE @throttleActiveRequestCount INT

    IF (@@TRANCOUNT > 0)
        THROW 50400, 'Cannot sleep in transaction', 1

    WHILE (1 = 1)
    BEGIN
        SELECT  @throttleCount = ISNULL(SUM(CASE
                                                WHEN r.wait_type IN ('IO_QUEUE_LIMIT', 'LOG_RATE_GOVERNOR', 'SE_REPL_CATCHUP_THROTTLE', 'SE_REPL_SLOW_SECONDARY_THROTTLE', 'HADR_SYNC_COMMIT') THEN 1
                                                ELSE 0
                                            END), 0),
                @sleepersCount = ISNULL(SUM(CASE
                                WHEN r.wait_type IN ('WAITFOR') THEN 1
                                ELSE 0
                            END), 0),
                @activeRequestCount = COUNT(*)
        FROM        sys.dm_exec_requests r WITH (NOLOCK)
        INNER JOIN  sys.dm_exec_sessions s WITH (NOLOCK)
        ON          s.session_id = r.session_id
        WHERE       r.session_id <> @@spid
                    AND s.is_user_process = 1 -- user sessions only

        SET @activeRequestCount = @activeRequestCount - @sleepersCount

        IF (@throttleCount > 0)
        BEGIN
            RAISERROR('Throttling due to write waits', 10, 0) WITH NOWAIT
            WAITFOR DELAY '00:00:02'
        END
        ELSE IF (@activeRequestCount >= 0)
        BEGIN
            -- Let us compute a value based on number of cores if sys.dm_os_sys_info is available.
            -- We want to compute this value once.
            IF (@throttleActiveRequestCount IS NULL)
            BEGIN TRY
                IF (OBJECT_ID('sys.dm_os_sys_info') IS NOT NULL)
                BEGIN
                    -- @throttleActiveRequestCount will be 3x # of cores, with 10 minimun and 100 maximum.
                    SELECT  @throttleActiveRequestCount = cpu_count * 3
                    FROM    sys.dm_os_sys_info

                    IF (@throttleActiveRequestCount < 10)
                    BEGIN
                        SET @throttleActiveRequestCount = 10
                    END
                    ELSE IF (@throttleActiveRequestCount > 100)
                    BEGIN
                        SET @throttleActiveRequestCount = 100
                    END
                END
            END TRY
            BEGIN CATCH
                -- Don't want to cause a failure if account does not have permission to query sys.dm_os_sys_info
            END CATCH

            IF (@throttleActiveRequestCount IS NULL)
            BEGIN
                SET @throttleActiveRequestCount = 20
            END

            IF (@activeRequestCount > @throttleActiveRequestCount)
            BEGIN
                RAISERROR('Throttling due to active requests being >= %d. Number of active requests = %d', 10, 0, @throttleActiveRequestCount, @activeRequestCount) WITH NOWAIT
                WAITFOR DELAY '00:00:01'
            END
            ELSE
            BEGIN
                BREAK
            END
        END
        ELSE
        BEGIN
            BREAK
        END
    END
END
GO

COMMIT TRANSACTION
