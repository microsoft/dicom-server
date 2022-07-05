# Azure Blob APIs

This document provides some context on different Azure blob SDK apis. 
https://github.com/Azure/azure-sdk-for-net/issues/22022

The `DownloadStreamingAsync` is replacement for `DownloadAsync`.
Except slightly different return type please think about this as a rename or new alias for the same functionality.
We've introduced `DownloadContentAsync` for scenarios where small sized blobs are used for formats supported by BinaryData type (e.g. json files) thus we wanted to rename existing API to make the download family less ambiguous.

The difference between `DownloadStreamingAsync` and `OpenReadAsync` is that the former gives you a network stream (wrapped with few layers but effectively think about it as network stream) which holds on to single connection, the later on the other hand fetches payload in chunks and buffers issuing multiple requests to fetch content.
Picking one over the other one depends on the scenario, i.e. if the consuming code is fast and you have good broad network link to storage account then former might be better choice as you avoid multiple req-res exchanges but if the consumer is slow then later might be a good idea as it releases a connection back to the pool right after reading and buffering next chunk. We recommend to perf test your app with both to reveal which is best choice if it's not obvious.
