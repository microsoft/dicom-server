/*************************************************************
    Stored procedures for updating content length of a file property
**************************************************************/
--
-- STORED PROCEDURE
--     UpdateFilePropertiesContentLength
--
-- DESCRIPTION
--     Update content length for a list of file properties matching the watermark.
--
-- PARAMETERS
--     @filePropertiesToUpdate
--         * A table type containing the file properties with content length that needs to be updated
--
-- RETURN VALUE
--     None
--
CREATE OR ALTER PROCEDURE dbo.UpdateFilePropertiesContentLength
    @filePropertiesToUpdate dbo.FilePropertyTableType_2 READONLY
AS
BEGIN
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        UPDATE FP
        SET ContentLength = FPTU.ContentLength
        FROM dbo.FileProperty FP
        JOIN @filePropertiesToUpdate FPTU
        ON FP.Watermark = FPTU.Watermark
        
    COMMIT TRANSACTION
END
