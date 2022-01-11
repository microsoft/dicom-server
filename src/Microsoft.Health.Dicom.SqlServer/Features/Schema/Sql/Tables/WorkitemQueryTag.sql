/*************************************************************
    Workitem Query Tag Table
    Stores static workitem indexed tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    QueryStatus can be 0, or 1 to represent Disabled or Enabled.
**************************************************************/
CREATE TABLE dbo.WorkitemQueryTag (
    TagKey                  INT                  NOT NULL, --PK
    TagPath                 VARCHAR(64)          NOT NULL,
    TagVR                   VARCHAR(2)           NOT NULL,
    QueryStatus             TINYINT              DEFAULT 1 NOT NULL,
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_WorkitemQueryTag ON dbo.WorkitemQueryTag
(
    TagKey
)

-- Used in GetExtendedQueryTag
CREATE UNIQUE NONCLUSTERED INDEX IXC_WorkitemQueryTag_TagPath ON dbo.WorkitemQueryTag
(
    TagPath
)
WITH (DATA_COMPRESSION = PAGE)
