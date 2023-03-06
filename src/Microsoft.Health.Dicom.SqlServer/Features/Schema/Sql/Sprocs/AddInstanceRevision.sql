/*************************************************************
    Stored procedure for adding an instance.
**************************************************************/
CREATE OR ALTER PROCEDURE dbo.AddInstanceRevision
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @seriesInstanceUid                  VARCHAR(64),
    @sopInstanceUid                     VARCHAR(64),
    @revision                           INT,
    @currentWatermark                   BIGINT,
    @nextWatermark                      BIGINT
AS
BEGIN
    SET NOCOUNT ON

    -- We turn off XACT_ABORT so that we can rollback and retry the INSERT/UPDATE into the study table on failure
    SET XACT_ABORT OFF

    BEGIN TRANSACTION

        DECLARE @currentDate DATETIME2(7) = SYSUTCDATETIME()
        DECLARE @existingStatus TINYINT
        DECLARE @newWatermark BIGINT
        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT
        DECLARE @transferSyntaxUid VARCHAR(64)
        DECLARE @hasFrameMetadata BIT

        SELECT @existingStatus = Status, @instanceKey = InstanceKey, @studyKey = StudyKey, @seriesKey = SeriesKey, @transferSyntaxUid = TransferSyntaxUid, @hasFrameMetadata = HasFrameMetadata
        FROM dbo.Instance
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid = @sopInstanceUid
            AND Revision = @revision
            AND Watermark = @currentWatermark

        IF @@ROWCOUNT = 0
            THROW 50409, 'Instance does not exists', @existingStatus;

        -- Insert Instance
        INSERT INTO dbo.Instance
            (PartitionKey, StudyKey, SeriesKey, InstanceKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Watermark, Status, LastStatusUpdatedDate, CreatedDate, TransferSyntaxUid, HasFrameMetadata, Revision, isFirstRevision, isLastRevision)
        VALUES
            (@partitionKey, @studyKey, @seriesKey, @instanceKey, @studyInstanceUid, @seriesInstanceUid, @sopInstanceUid, @nextWatermark, @existingStatus, @currentDate, @currentDate, @transferSyntaxUid, @hasFrameMetadata, @revision + 1, 0, 1)

    COMMIT TRANSACTION
END
