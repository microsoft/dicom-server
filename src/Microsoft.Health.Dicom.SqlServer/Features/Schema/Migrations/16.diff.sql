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

    IF SERVERPROPERTY('Edition') = 'SQL Azure'
    BEGIN
        IF (@@TRANCOUNT > 0)
            THROW 50400, 'Cannot sleep in transaction', 1

        DECLARE @computedThrottleActiveRequestCount INT

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
            FROM    sys.dm_exec_requests r WITH (NOLOCK)
            JOIN    sys.dm_exec_sessions s WITH (NOLOCK)
            ON      s.session_id = r.session_id
            WHERE   r.session_id <> @@spid
                    AND s.is_user_process = 1 -- user sessions only

            SET @activeRequestCount = @activeRequestCount - @sleepersCount

            IF (@throttleCount > 0)
            BEGIN
                RAISERROR('Throttling due to write waits', 10, 0) WITH NOWAIT
                WAITFOR DELAY '00:00:02'
            END
            ELSE IF (@activeRequestCount >= 0)
            BEGIN
                SET @throttleActiveRequestCount = NULL

                -- Check if we have a setting in the tbl_ResourceManagementSetting table.
                -- We need to check this setting on every iteration of the loop, to make ensure that we can increase the value if prc_iSleepIfBusy is stuck.
                SELECT  @throttleActiveRequestCount = CONVERT(INT, Value)
                FROM    tbl_ResourceManagementSetting
                WHERE   Name = 'ThrottleActiveRequestCount'

                IF (@throttleActiveRequestCount IS NULL)
                BEGIN
                    -- ThrottleActiveRequestCount is not set.
                    -- Let us compute a value based on number of cores if sys.dm_os_sys_info is available.
                    -- We want to compute this value once.
                    IF (@computedThrottleActiveRequestCount IS NULL)
                    BEGIN TRY
                        IF (OBJECT_ID('sys.dm_os_sys_info') IS NOT NULL)
                        BEGIN
                            -- @computedThrottleActiveRequestCount will be 3x # of cores, with 10 minimun and 100 maximum.
                            SELECT  @computedThrottleActiveRequestCount = cpu_count * 3
                            FROM    sys.dm_os_sys_info

                            IF (@computedThrottleActiveRequestCount < 10)
                            BEGIN
                                SET @computedThrottleActiveRequestCount = 10
                            END
                            ELSE IF (@computedThrottleActiveRequestCount > 100)
                            BEGIN
                                SET @computedThrottleActiveRequestCount = 100
                            END
                        END
                    END TRY
                    BEGIN CATCH
                        -- Don't want to cause a failure if account does not have permission to query sys.dm_os_sys_info
                    END CATCH

                    IF (@computedThrottleActiveRequestCount IS NULL)
                    BEGIN
                        SET @computedThrottleActiveRequestCount = 20
                    END

                    SET @throttleActiveRequestCount = @computedThrottleActiveRequestCount
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
    WHILE (1 = 1)
    BEGIN
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

        IF (@@ROWCOUNT <> @batchSize)
            BREAK

        EXEC dbo.ISleepIfBusy
    END

    -- Delete errors
    WHILE (1 = 1)
    BEGIN
        DELETE TOP (@batchSize) FROM dbo.ExtendedQueryTagError
        WHERE TagKey = @tagKey

        IF (@@ROWCOUNT <> @batchSize)
            BREAK

        EXEC dbo.ISleepIfBusy
    END

    -- Delete tag
    DELETE FROM dbo.ExtendedQueryTag
    WHERE TagKey = @tagKey
END
GO

COMMIT TRANSACTION

/****************************************************************************************
Delete Decimal String (DS) and Integer String (IS) Tags
******************************************************************************************/
DECLARE @tagPath VARCHAR(64)

WHILE (1 = 1)
BEGIN
    -- Get next tag
    SELECT TOP 1 @tagPath = TagPath
    FROM dbo.ExtendedQueryTag
    WHERE TagVR = 'DS' OR TagVR = 'IS'

    IF @@ROWCOUNT = 0
        BREAK

    BEGIN TRY
        EXEC dbo.DeleteExtendedQueryTagV16 @tagPath, 0
    END TRY
    BEGIN CATCH
        -- Ignore any errors
    END CATCH
END
