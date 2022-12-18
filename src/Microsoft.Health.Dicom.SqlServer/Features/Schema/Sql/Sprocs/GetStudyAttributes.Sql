/***************************************************************************************/
-- STORED PROCEDURE
--     Get Study level QIDO response
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetStudyAttributes (
	@partitionKey       INT,
	@studyInstanceUidTableType dbo.StudyInstanceUidTableType READONLY
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  sv.*
    FROM    dbo.StudyResponseView sv
	JOIN    @studyInstanceUidTableType input
    ON      partitionKey = @partitionKey
	AND     sv.StudyInstanceUid = input.StudyInstanceUid
END
