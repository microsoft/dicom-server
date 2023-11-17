SET XACT_ABORT ON

BEGIN TRANSACTION
/*************************************************************
    Table Updates
**************************************************************/

/*************************************************************
    Study Table
    Make PatientId Nullable
      You can specify NULL in ALTER COLUMN to force a NOT NULL column to allow null values.
      If NULL or NOT NULL is specified with ALTER COLUMN, new_data_type [(precision [, scale ])] must also be specified.
      If the data type, precision, and scale are not changed, specify the current column values.
**************************************************************/
IF EXISTS 
(
    SELECT *
    FROM    sys.columns
    WHERE   (NAME = 'PatientId')
        AND Object_id = OBJECT_ID('dbo.Study')
)
BEGIN
    ALTER TABLE dbo.Study ALTER COLUMN PatientId NVARCHAR (64) NULL;
END