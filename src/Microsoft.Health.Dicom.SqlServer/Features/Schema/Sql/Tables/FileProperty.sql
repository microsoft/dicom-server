/*************************************************************
    File Property Table
    Stores file properties of a given instance
**************************************************************/
CREATE TABLE dbo.FileProperty (
    InstanceKey     BIGINT          NOT NULL,  -- FK to Instance table
    -- Watermark is a unique identifier used to differentiate between one version of a file from another for the same 
    -- instance.
    Watermark       BIGINT          NOT NULL,
    -- Since blob names can be up to 1,024 characters when hierarchical naming used, we can set the column to max 
    -- bytes to accommodate up to potentially 2 bytes for c-strings and allow for larger paths if blob max changes.
    FilePath        NVARCHAR (4000) NOT NULL,
    -- ETag is of unspecified size and its generation is opaque to us. In practice, these are typically short hashes 
    -- no more than 100 characters long. We will use max available as it is a varchar.
    ETag            NVARCHAR (4000)  NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_FileProperty ON dbo.FileProperty(
    InstanceKey,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE, ONLINE = ON)
