# Indexes

This document provides context on how the SQL indexes are organized on core tables.

## Context

Study, Series and Instance table all have PartitionKey, *Keys and *Uid's. 
- `*Uids` are customer generated unique ids. 
- `*Keys` are system generated primary keys. Key's are smaller and used for internal SQL joins only. Should not be distributed to customers.
- `PartitionKey` allows logical grouping of data per partition. Losely held, no strong enforcement. Need to be extremely carefull to include in all filtering and exceptions should be carefully reviewed.

## Best Practices

- All indexes will use PartitionKey as the first column. All SQL filtering should include PartitionKey. Cross partition QIDO is not supported today. In the future we can support in Analytics.
- Exceptions like ExtendQueryTag/ChangeFeed/Re-Indexing which works across all partition, will have indexes on watermark that will be cross partition.
- Indexes on both Uids and Keys will be supported. Uids are used during STOW and WADO from the customers. Keys are used for internal use only for joins.

### Diff indexes outside of transaction

All indexes should be created outside of transaction and after the transaction has been committed. Having it inside the transaction
will lock the table during creation and if it's a bigger table it'll cause issues.
Even though ONLINE = ON allows most operations to continue while the index is being created, a short-term shared lock is still taken at the start of the operation, 
and an exclusive lock is taken at the end. This means that other transactions cannot modify the data during these periods, so move the index to outside of
transaction.
Be sure to have the GO statement after the index, not after the begin transaction statement.

```sql
SET XACT_ABORT ON
BEGIN TRANSACTION
GO
/*************************************************************
    Stored procedures and other things can be within trasnaction
**************************************************************/
      
COMMIT TRANSACTION


IF NOT EXISTS 
(
    SELECT *
    FROM    sys.indexes
    WHERE   NAME = 'IXC_FileProperty_InstanceKey_Watermark_ContentLength'
        AND Object_id = OBJECT_ID('dbo.FileProperty')
)
BEGIN
    CREATE NONCLUSTERED INDEX IXC_FileProperty_InstanceKey_Watermark_ContentLength ON dbo.FileProperty
    (
    InstanceKey,
    Watermark,
    ContentLength
    ) WITH (DATA_COMPRESSION = PAGE, ONLINE = ON)
END
GO
```
