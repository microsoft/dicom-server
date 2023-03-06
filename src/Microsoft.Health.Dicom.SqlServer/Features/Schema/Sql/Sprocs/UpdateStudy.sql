/*************************************************************
    Stored procedure for updating a study.
**************************************************************/
CREATE OR ALTER PROCEDURE dbo.UpdateStudy
    @partitionKey                       INT,
    @studyInstanceUid                   VARCHAR(64),
    @patientId                          NVARCHAR(64),
    @patientName                        NVARCHAR(325) = NULL,
    @referringPhysicianName             NVARCHAR(325) = NULL,
    @studyDate                          DATE = NULL,
    @studyDescription                   NVARCHAR(64) = NULL,
    @accessionNumber                    NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON

    -- We turn off XACT_ABORT so that we can rollback and retry the INSERT/UPDATE into the study table on failure
    SET XACT_ABORT OFF

    BEGIN TRANSACTION

        DECLARE @studyKey BIGINT
            
        SELECT @studyKey = StudyKey
        FROM dbo.Study WITH(UPDLOCK)
        WHERE PartitionKey = @partitionKey
            AND StudyInstanceUid = @studyInstanceUid

        IF @@ROWCOUNT = 0
        THROW 50409, 'Study update failed.', 1;

        UPDATE dbo.Study
            SET PatientId = @patientId, PatientName = @patientName, ReferringPhysicianName = @referringPhysicianName, StudyDate = @studyDate, StudyDescription = @studyDescription, AccessionNumber = @accessionNumber
            WHERE PartitionKey = @partitionKey
                AND StudyKey = @studyKey

    COMMIT TRANSACTION
END
