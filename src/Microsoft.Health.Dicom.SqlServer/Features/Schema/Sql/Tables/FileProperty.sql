/*************************************************************
    File Property Table
    Stores file properties of a given instance
**************************************************************/
CREATE TABLE dbo.FileProperty (
                                  InstanceKey             BIGINT             NOT NULL, --FK
                                  FilePath                NVARCHAR(4000)     NOT NULL,
                                  ETag                    NVARCHAR(200)      NOT NULL
)
WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_FileProperty ON dbo.FileProperty(InstanceKey)
