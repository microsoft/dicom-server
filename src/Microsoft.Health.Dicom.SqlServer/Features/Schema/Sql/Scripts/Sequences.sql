/*************************************************************
    Sequence for generating sequential unique ids
**************************************************************/

CREATE SEQUENCE dbo.WatermarkSequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.StudyKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.SeriesKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

CREATE SEQUENCE dbo.InstanceKeySequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 1000000

 -- used for both ExtendedQueryTag and WorkitemQueryTag to allow
 -- ExtendedQueryTag* data tables to have TagKey references to both tables
CREATE SEQUENCE dbo.TagKeySequence
    AS INT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000

CREATE SEQUENCE dbo.PartitionKeySequence
    AS INT
    START WITH 2    -- skipping the default partition
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000

CREATE SEQUENCE dbo.WorkitemKeySequence
    AS INT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000

CREATE SEQUENCE dbo.WorkitemWatermarkSequence
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO CYCLE
    CACHE 10000
