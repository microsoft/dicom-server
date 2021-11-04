# Data Partitions

> For a full discussion of this feature, see [this document](https://dev.azure.com/microsofthealth/Health/_wiki/wikis/Resolute.wiki/721/MultiTenancy).

## Feature Overview
The data partitions feature is set via configuration. Our current approach is to disallow the feature from being **disabled.**

On startup, we throw an exception if:
 - the feature is disabled **and**
 - a partition other than the default partition (`Microsoft.Default`) exists

You can send requests to the App Service we've set up, or feel free to create your own by deploying to Azure, setting
the App Setting `DicomServer:Features:EnableDataPartitions` to `true`, and restarting the App Service.

## API Changes
When enabled, the data partitions feature requires prepending `/partitions/<partitionName>` to STOW, WADO, QIDO, and delete requests. Change feed and
extended query tag, and operation endpoints do not require a partition to be specified. Cross-partition query is not supported.

> A full set of sample requests can be found in this branch's [Partition-aware Postman Collection](../resources/Conformance-as-Postman.postman_collection.json)

## Partition Lifecycle
For this iteration of the feature, partitions are created implicitly by sending a valid STOW request that specifies a [valid partition name.](../../src/Microsoft.Health.Dicom.Core/Features/Validation/PartitionNameValidator.cs)
There is no way to update or delete partitions, but the list of partitions can be retrieved at `/partitions` to ensure discoverability of all resources.

## Core Scenario
The basic scenario would be storing the same file under multiple partitions, and then validating the expected responses from WADO and QIDO. We've included
extended query tag requests in the Postman collection to test that functionality.

## Extended Scenarios
Please try to find things we may have overlooked! Edge cases, limits, performance, usability - we want to hear your thoughts!
