/***************************************************************************************/
-- STORED PROCEDURE
--     GetTotalAndSumContentLengthIndexedAsyncV57
--
-- FIRST SCHEMA VERSION
--     57
--
-- DESCRIPTION
--     Retrieves total sum of content length across all FileProperty rows
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetTotalAndSumContentLengthIndexedAsyncV57
    AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

SELECT count(*), SUM(ContentLength) FROM dbo.FileProperty
END