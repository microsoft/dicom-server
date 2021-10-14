/***************************************************************************************/
-- STORED PROCEDURE
--     DeleteDeletedInstanceV6
--
-- FIRST SCHEMA VERSION
--     6
--
-- DESCRIPTION
--     Removes a deleted instance from the DeletedInstance table
--
-- PARAMETERS
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
--     @watermark
--         * The watermark of the entry
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.DeleteDeletedInstanceV6(
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64),
    @sopInstanceUid     VARCHAR(64),
    @watermark          BIGINT
)
AS
    SET NOCOUNT ON

    DELETE
    FROM    dbo.DeletedInstance
    WHERE   PartitionKey = @partitionKey
        AND     StudyInstanceUid = @studyInstanceUid
        AND     SeriesInstanceUid = @seriesInstanceUid
        AND     SopInstanceUid = @sopInstanceUid
        AND     Watermark = @watermark
