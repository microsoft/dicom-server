/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md
This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - IMPORTANT: Avoid rebuiling indexes inside the transaction, it locks the table during the transaction.
******************************************************************************************/

SET XACT_ABORT ON

BEGIN TRANSACTION

-- Data Type INT = 56
IF EXISTS (SELECT * FROM sys.sequences WHERE Name = 'WorkitemKeySequence' AND system_type_id = 56)
BEGIN

    DECLARE @sql NVARCHAR(MAX);
    DECLARE @newWorkitemKeyStartVal INT = 1;

    -- Daily average is at ~50. Hence we buffer 100 on top of the last workitem key sequence
    IF EXISTS(SELECT * FROM Workitem)
        SELECT
            @newWorkitemKeyStartVal = MAX(WorkitemKey) + 100
        FROM
            dbo.Workitem

    SET @sql = CONCAT(N'
        BEGIN TRANSACTION
        DROP SEQUENCE dbo.WorkitemKeySequence
        CREATE SEQUENCE dbo.WorkitemKeySequence AS BIGINT ' +
            'START WITH ' + STR(@newWorkitemKeyStartVal),
            'INCREMENT BY 1' +
			'MINVALUE ' + STR(@newWorkitemKeyStartVal) +
            'NO CYCLE CACHE 10000' +
        'COMMIT TRANSACTION');

    EXEC sys.sp_executesql @sql;

END
GO

COMMIT TRANSACTION
