# Pull DICOM changes using the Change Feed

The Change Feed offers customers the ability to go through the history of the Medical Imaging Server for DICOM and act on the create and delete events in the service. This How-to Guide shows you how to consume Change Feed.

The Change Feed is accessed using REST APIs documented in [Change Feed Concept](/docs/concepts/change-feed.md), it also provides example usage of Change Feed.

## Consume Change Feed

Sample C# code using the provided DICOM client package:

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

You can find the full code available here: [Consume Change Feed](../../converter/dicom-cast/src/Microsoft.Health.DicomCast.Core/Features/DicomWeb/Service/ChangeFeedRetrieveService.cs)

## Summary

This How-to Guide demonstrates how to consume Change Feed. Change Feed allows you to monitor the history of the Medical Imaging Server for DICOM. To learn more about Change Feed, refer to the [Change Feed Concept](../concepts/change-feed.md).

### Next Steps

DICOM Cast polls for any changes via Change Feed, which allows synchronizing the data from a Medical Imaging Server for DICOM to an Azure API for FHIR server. To learn more DICOM Cast, refer to the [DICOM Cast Concept](../concepts/dicom-cast.md).
