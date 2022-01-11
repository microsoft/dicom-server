/*************************************************************
    Extended Query Tag Table
    Stores added extended query tags
    TagPath is represented without any delimiters and each level takes 8 bytes
    TagLevel can be 0, 1 or 2 to represent Instance, Series or Study level
    TagPrivateCreator is identification code of private tag implementer, only apply to private tag.
    TagStatus can be 0, 1 or 2 to represent Adding, Ready or Deleting.
    QueryStatus can be 0, or 1 to represent Disabled or Enabled.
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTag (
    TagKey                  INT                  NOT NULL, --PK
    TagPath                 VARCHAR(64)          NOT NULL,
    TagVR                   VARCHAR(2)           NOT NULL,
    TagPrivateCreator       NVARCHAR(64)         NULL,
    TagLevel                TINYINT              NOT NULL,
    TagStatus               TINYINT              NOT NULL,
    QueryStatus             TINYINT              DEFAULT 1 NOT NULL,
    ErrorCount              INT                  DEFAULT 0 NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTag ON dbo.ExtendedQueryTag
(
    TagKey
)

-- Used in GetExtendedQueryTag
CREATE UNIQUE NONCLUSTERED INDEX IX_ExtendedQueryTag_TagPath ON dbo.ExtendedQueryTag
(
    TagPath
)
