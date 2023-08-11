SET XACT_ABORT ON
BEGIN TRANSACTION

/*************************************************************
    FilePropertyTableType TableType
    To use when inserting rows of FileProperty
**************************************************************/
IF TYPE_ID(N'FilePropertyTableType') IS NULL
BEGIN
CREATE TYPE dbo.FilePropertyTableType AS TABLE
(
    Watermark BIGINT          NOT NULL INDEX IXC_FilePropertyTableType CLUSTERED,
    FilePath  NVARCHAR (4000) NOT NULL,
    ETag      NVARCHAR (4000) NOT NULL
)
END
GO

/*************************************************************
    sproc updates
**************************************************************/

/*************************************************************
    EndUpdateInstanceV44 altered to take in rows of
    FileProperty from all of the updates blobs and insert them
    into table. Prior to this insertion, we delete any rows on
    FileProperty that do not have original watermark as they
    will be stale and pointing to files that no longer exist.
**************************************************************/

CREATE OR ALTER PROCEDURE dbo.EndUpdateInstanceV44
@partitionKey INT, @studyInstanceUid VARCHAR (64), @patientId NVARCHAR (64)=NULL, @patientName NVARCHAR (325)=NULL, @patientBirthDate DATE=NULL, @insertFileProperties dbo.FilePropertyTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @currentDate AS DATETIME2 (7) = SYSUTCDATETIME();
    CREATE TABLE #UpdatedInstances (
        PartitionKey      INT         ,
        StudyInstanceUid  VARCHAR (64),
        SeriesInstanceUid VARCHAR (64),
        SopInstanceUid    VARCHAR (64),
        Watermark         BIGINT      ,
        OriginalWatermark BIGINT      ,
        InstanceKey       BIGINT
    );
    DELETE #UpdatedInstances;
    UPDATE dbo.Instance
    SET    LastStatusUpdatedDate = @currentDate,
           OriginalWatermark     = ISNULL(OriginalWatermark, Watermark),
           Watermark             = NewWatermark,
           NewWatermark          = NULL
    OUTPUT deleted.PartitionKey, @studyInstanceUid, deleted.SeriesInstanceUid, deleted.SopInstanceUid, inserted.Watermark, inserted.OriginalWatermark, deleted.InstanceKey INTO #UpdatedInstances
    WHERE PartitionKey = @partitionKey
      AND StudyInstanceUid = @studyInstanceUid
      AND Status = 1
      AND NewWatermark IS NOT NULL;
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstances')
        CREATE UNIQUE INDEX IXC_UpdatedInstances
            ON #UpdatedInstances(Watermark);
    IF NOT EXISTS (SELECT *
                   FROM   tempdb.sys.indexes
                   WHERE  name = 'IXC_UpdatedInstanceKeyWatermark')
        CREATE UNIQUE CLUSTERED INDEX IXC_UpdatedInstanceKeyWatermark
            ON #UpdatedInstances(InstanceKey, OriginalWatermark);
    UPDATE dbo.Study
    SET    PatientId        = ISNULL(@patientId, PatientId),
           PatientName      = ISNULL(@patientName, PatientName),
           PatientBirthDate = ISNULL(@patientBirthDate, PatientBirthDate)
    WHERE  PartitionKey = @partitionKey
           AND StudyInstanceUid = @studyInstanceUid;
    IF @@ROWCOUNT = 0
        THROW 50404, 'Study does not exist', 1;
    IF EXISTS (SELECT 1
               FROM   @insertFileProperties)
        DELETE FP
        FROM   dbo.FileProperty AS FP
               INNER JOIN
               #UpdatedInstances AS U
               ON U.InstanceKey = FP.InstanceKey
        WHERE  U.OriginalWatermark != FP.Watermark;
    INSERT INTO dbo.FileProperty (InstanceKey, Watermark, FilePath, ETag)
    SELECT U.InstanceKey,
           I.Watermark,
           I.FilePath,
           I.ETag
    FROM   @insertFileProperties AS I
           INNER JOIN
           #UpdatedInstances AS U
           ON U.Watermark = I.Watermark;
    INSERT INTO dbo.ChangeFeed (Action, PartitionKey, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, OriginalWatermark)
    SELECT 2,
           PartitionKey,
           StudyInstanceUid,
           SeriesInstanceUid,
           SopInstanceUid,
           Watermark
    FROM   #UpdatedInstances;
    UPDATE C
    SET    CurrentWatermark = U.Watermark,
           FilePath         = I.FilePath
    FROM   dbo.ChangeFeed AS C
           INNER JOIN
           #UpdatedInstances AS U
           ON C.PartitionKey = U.PartitionKey
              AND C.StudyInstanceUid = U.StudyInstanceUid
              AND C.SeriesInstanceUid = U.SeriesInstanceUid
              AND C.SopInstanceUid = U.SopInstanceUid
           LEFT OUTER JOIN
           @insertFileProperties AS I
           ON I.Watermark = U.Watermark;
    COMMIT TRANSACTION;
END

GO

COMMIT TRANSACTION
