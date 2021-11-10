/****************************************************************************************
Guidelines to create migration scripts - https://github.com/microsoft/healthcare-shared-components/tree/master/src/Microsoft.Health.SqlServer/SqlSchemaScriptsGuidelines.md

This diff is broken up into several sections:
 - The first transaction contains changes to tables and stored procedures.
 - The second transaction contains updates to indexes.
 - After the second transaction, there's an update to a full-text index which cannot be in a transaction.
******************************************************************************************/
SET XACT_ABORT ON

BEGIN TRANSACTION
BEGIN TRY
-- wrapping the contents of this transaction in try/catch because errors on index
-- operations won't rollback unless caught and re-thrown
    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_Instance_SeriesKey_Status'
            AND Object_id = OBJECT_ID('dbo.Instance')
    )
    BEGIN
        DROP INDEX IX_Instance_SeriesKey_Status ON dbo.Instance

        CREATE NONCLUSTERED INDEX IX_Instance_SeriesKey_Status_Watermark on dbo.Instance
        (
            SeriesKey,
            Status,
            Watermark
        )
        INCLUDE
        (
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    IF EXISTS 
    (
        SELECT *
        FROM    sys.indexes
        WHERE   NAME = 'IX_Instance_StudyKey_Status'
            AND Object_id = OBJECT_ID('dbo.Instance')
    )
    BEGIN
        DROP INDEX IX_Instance_StudyKey_Status ON dbo.Instance

        CREATE NONCLUSTERED INDEX IX_Instance_StudyKey_Status_Watermark on dbo.Instance
        (
            StudyKey,
            Status,
            Watermark
        )
        INCLUDE
        (
            StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid
        )
        WITH (DATA_COMPRESSION = PAGE)
    END

    COMMIT TRANSACTION

END TRY
BEGIN CATCH
    ROLLBACK;
    THROW;
END CATCH
