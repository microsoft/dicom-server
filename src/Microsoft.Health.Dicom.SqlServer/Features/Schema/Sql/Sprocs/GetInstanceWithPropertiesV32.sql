/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceWithPropertiesV32
--
-- FIRST SCHEMA VERSION
--     32
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level with additional instance properties
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
--     @partitionKey
--         * The Partition key
--     @studyInstanceUid
--         * The study instance UID.
--     @seriesInstanceUid
--         * The series instance UID.
--     @sopInstanceUid
--         * The SOP instance UID.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV32 (
    @validStatus        TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON


    SELECT  StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            Watermark,
            TransferSyntaxUid,
            HasFrameMetadata,
            OriginalWatermark,
            NewWatermark
    FROM    dbo.Instance
    WHERE   PartitionKey            = @partitionKey
            AND StudyInstanceUid    = @studyInstanceUid
            AND SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
            AND SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
            AND Status              = @validStatus

END
