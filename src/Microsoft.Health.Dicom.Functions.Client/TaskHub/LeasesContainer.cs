// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal class LeasesContainer
{
    private readonly BlobContainerClient _containerClient;

    internal const string TaskHubBlobName = "taskhub.json";

    public LeasesContainer(BlobServiceClient blobServiceClient, string taskHubName)
        => _containerClient = EnsureArg
            .IsNotNull(blobServiceClient, nameof(blobServiceClient))
            .GetBlobContainerClient(GetName(taskHubName));

    public string Name => _containerClient.Name;

    public virtual async ValueTask<TaskHubInfo> GetTaskHubInfoAsync(CancellationToken cancellationToken = default)
    {
        BlobClient client = _containerClient.GetBlobClient(TaskHubBlobName);

        try
        {
            BlobDownloadResult result = await client.DownloadContentAsync(cancellationToken);
            return result.Content.ToObjectFromJson<TaskHubInfo>();
        }
        catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#container-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Blob container names must be lowercase.")]
    internal static string GetName(string taskHub)
        => taskHub?.ToLowerInvariant() + "-leases";
}
