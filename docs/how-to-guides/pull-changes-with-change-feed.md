# Pull DICOM changes using the Change Feed

The change feed offers the customers the ability to go through the history of the DICOM server and act on the create and delete events in the service.

Change feed is accessed using REST APIs documented [here](/docs/Resources/ChangeFeed.md), it also provides example usage of change feed.

## Consume Change Feed

Sample C# code using the provided DICOM client package, full code is available at
converter/dicom-cast/src/Microsoft.Health.DicomCast.Core/Features/DicomWeb/Service/ChangeFeedRetrieveService.cs

```csharp
public async Task<IReadOnlyList<ChangeFeedEntry>> RetrieveChangeFeedAsync(long offset, CancellationToken cancellationToken)
{
    var _dicomWebClient = new DicomWebClient(
                    new HttpClient { BaseAddress = dicomWebConfiguration.Endpoint },
                    sp.GetRequiredService<RecyclableMemoryStreamManager>(),
                    tokenUri: null);
    DicomWebResponse<IReadOnlyList<ChangeFeedEntry>> result = await _dicomWebClient.GetChangeFeed(
    $"?offset={offset}&limit={DefaultLimit}&includeMetadata={true}",
    cancellationToken);

    if (result?.Value != null)
    {
            return result.Value;
    }

    return Array.Empty<ChangeFeedEntry>();
}
```







