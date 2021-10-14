/***************************************************************************************/
-- STORED PROCEDURE
--     GetPartition
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Gets the partition for the specified name
--
-- PARAMETERS
--     @partitionName
--         * Client provided partition name
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetPartition (
    @partitionName   VARCHAR(64)
) AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  PartitionKey,
            PartitionName,
            CreatedDate
    FROM    dbo.Partition
    WHERE PartitionName = @partitionName
END
