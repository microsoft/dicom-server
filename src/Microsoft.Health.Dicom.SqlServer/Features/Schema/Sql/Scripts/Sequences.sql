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
