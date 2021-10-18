/***************************************************************************************/
-- STORED PROCEDURE
--     GetPartitions
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets all data partitions
--
-- PARAMETERS
--
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetPartitions AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  PartitionKey,
            PartitionName,
            CreatedDate
    FROM    dbo.Partition
END
