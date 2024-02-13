/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstanceWithPropertiesV46
--
-- FIRST SCHEMA VERSION
--     58
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
--     @originalVersion
--         * If true, will return original file properties.
/***************************************************************************************/
CREATE OR ALTER PROCEDURE dbo.GetInstanceWithPropertiesV58 (
    @validStatus        TINYINT,
    @partitionKey       INT,
    @studyInstanceUid   VARCHAR(64),
    @seriesInstanceUid  VARCHAR(64) = NULL,
    @sopInstanceUid     VARCHAR(64) = NULL,
    @originalVersion    BIT = 0
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON


    SELECT  i.StudyInstanceUid,
            i.SeriesInstanceUid,
            i.SopInstanceUid,
            i.Watermark,
            i.TransferSyntaxUid,
            i.HasFrameMetadata,
            i.OriginalWatermark,
            i.NewWatermark,
            f.FilePath,
            f.ETag
    FROM    dbo.Instance as i
    LEFT OUTER JOIN
            dbo.FileProperty AS f
            ON f.InstanceKey = i.InstanceKey
            AND f.Watermark = IIF(@originalVersion = 1, ISNULL(i.OriginalWatermark, i.Watermark), i.Watermark)
    WHERE i.PartitionKey            = @partitionKey
      AND i.StudyInstanceUid    = @studyInstanceUid
      AND i.SeriesInstanceUid   = ISNULL(@seriesInstanceUid, SeriesInstanceUid)
      AND i.SopInstanceUid      = ISNULL(@sopInstanceUid, SopInstanceUid)
      AND i.Status              = @validStatus

END
