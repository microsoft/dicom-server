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
