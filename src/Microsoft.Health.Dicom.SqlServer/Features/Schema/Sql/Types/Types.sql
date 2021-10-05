
/*************************************************************
    The user defined type for AddExtendedQueryTagsInput
*************************************************************/
CREATE TYPE dbo.AddExtendedQueryTagsInputTableType_1 AS TABLE
(
    TagPath                    VARCHAR(64),  -- Extended Query Tag Path. Each extended query tag take 8 bytes, support upto 8 levels, no delimeter between each level.
    TagVR                      VARCHAR(2),  -- Extended Query Tag VR.
    TagPrivateCreator          NVARCHAR(64),  -- Extended Query Tag Private Creator, only valid for private tag.
    TagLevel                   TINYINT  -- Extended Query Tag level. 0 -- Instance Level, 1 -- Series Level, 2 -- Study Level
)

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type String
*************************************************************/
CREATE TYPE dbo.InsertStringExtendedQueryTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   NVARCHAR(64),
    TagLevel                   TINYINT
)

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Double
*************************************************************/
CREATE TYPE dbo.InsertDoubleExtendedQueryTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   FLOAT(53),
    TagLevel                   TINYINT
)

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Long
*************************************************************/
CREATE TYPE dbo.InsertLongExtendedQueryTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   BIGINT,
    TagLevel                   TINYINT
)

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Date Time
*************************************************************/
CREATE TYPE dbo.InsertDateTimeExtendedQueryTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   DATETIME2(7),
    TagLevel                   TINYINT
)

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Date Time.
    V2 contains the TagValueUtc which separates it from V1.
*************************************************************/
CREATE TYPE dbo.InsertDateTimeExtendedQueryTagTableType_2 AS TABLE
(
    TagKey                     INT,
    TagValue                   DATETIME2(7),
    TagValueUtc                DATETIME2(7)         NULL,
    TagLevel                   TINYINT
)

/*************************************************************
    Table valued parameter to insert into Extended Query Tag table for data type Person Name
*************************************************************/
CREATE TYPE dbo.InsertPersonNameExtendedQueryTagTableType_1 AS TABLE
(
    TagKey                     INT,
    TagValue                   NVARCHAR(200)        COLLATE SQL_Latin1_General_CP1_CI_AI,
    TagLevel                   TINYINT
)

/*************************************************************
    The user defined type for stored procedures that consume extended query tag keys
*************************************************************/
CREATE TYPE dbo.ExtendedQueryTagKeyTableType_1 AS TABLE
(
    TagKey                     INT
)
