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
