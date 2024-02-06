/***************************************************************************************/
-- STORED PROCEDURE
--     GetIndexedFileMetrics
--
-- FIRST SCHEMA VERSION
--     57
--
-- DESCRIPTION
--     Retrieves total sum of content length across all FileProperty rows
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetIndexedFileMetrics
    AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

SELECT TotalIndexedFileCount=COUNT_BIG(*), TotalIndexedBytes=SUM(ContentLength) FROM dbo.FileProperty
END