SET XACT_ABORT ON

BEGIN TRANSACTION
GO

/*************************************************************
    Stored procedures for adding the partition.
**************************************************************/
--
-- STORED PROCEDURE
--     AddPartition
--
-- DESCRIPTION
--     Adds a partition if it doesnot exists.
--
-- PARAMETERS
--     @partitionName
--         * The data partition to be created.
CREATE OR ALTER PROCEDURE dbo.AddPartition
@partitionName VARCHAR (64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @createdDate AS DATETIME2 (7) = SYSUTCDATETIME();
    DECLARE @partitionKey AS INT;
    SELECT @partitionKey = PartitionKey
    FROM   dbo.Partition  WITH (UPDLOCK)
    WHERE  PartitionName = @partitionName;
    IF @@ROWCOUNT <> 0
        THROW 50409, 'Partition already exists', 1;
    SET @partitionKey =  NEXT VALUE FOR dbo.PartitionKeySequence;
    INSERT  INTO dbo.Partition (PartitionKey, PartitionName, CreatedDate)
    VALUES                    (@partitionKey, @partitionName, @createdDate);
    SELECT @partitionKey,
           @partitionName,
           @createdDate;
    COMMIT TRANSACTION;
END
GO
COMMIT TRANSACTION
