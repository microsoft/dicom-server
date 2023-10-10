SET XACT_ABORT ON

BEGIN TRANSACTION

/*************************************************************
    DeletedInstance Table
    Add FilePath and ETag nullable columns
**************************************************************/
IF NOT EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   (NAME = 'FilePath')
        AND Object_id = OBJECT_ID('dbo.DeletedInstance')
)
BEGIN
    ALTER TABLE dbo.DeletedInstance
    ADD FilePath NVARCHAR(4000) NULL,
        ETag NVARCHAR(4000) NULL
END
GO

/*************************************************************
    sproc updates
**************************************************************/
