/*************************************************************
    Workitem Query Tag Table
    Stores static workitem indexed tags
    TagPath is represented with delimiters to repesent multiple levels
**************************************************************/
CREATE TABLE dbo.WorkitemQueryTag (
    TagKey                  INT                  NOT NULL, --PK
    TagPath                 VARCHAR(64)          NOT NULL,
    TagVR                   VARCHAR(2)           NOT NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_WorkitemQueryTag ON dbo.WorkitemQueryTag
(
    TagKey
)


CREATE UNIQUE NONCLUSTERED INDEX IXC_WorkitemQueryTag_TagPath ON dbo.WorkitemQueryTag
(
    TagPath
)
WITH (DATA_COMPRESSION = PAGE)
