
/*************************************************************
    Extended Query Tag Operation Table
    Stores the association between tags and their reindexing operation
    TagKey is the primary key and foreign key for the row in dbo.ExtendedQueryTag
    OperationId is the unique ID for the associated operation (like reindexing)
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagOperation (
    TagKey                  INT                  NOT NULL, --PK
    OperationId             uniqueidentifier     NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagOperation ON dbo.ExtendedQueryTagOperation
(
    TagKey
)

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagOperation_OperationId ON dbo.ExtendedQueryTagOperation
(
    OperationId
)
INCLUDE
(
    TagKey
)
