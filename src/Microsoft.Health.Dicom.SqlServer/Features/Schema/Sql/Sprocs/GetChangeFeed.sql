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
CREATE OR ALTER PROCEDURE dbo.GetChangeFeed (
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