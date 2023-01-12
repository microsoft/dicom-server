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
    private readonly BlobServiceClient _blobServiceClient;

    internal const string TaskHubBlobName = "taskhub.json";

    public LeasesContainer(BlobServiceClient blobServiceClient)
        => _blobServiceClient = EnsureArg.IsNotNull(blobServiceClient, nameof(blobServiceClient));

    public async ValueTask<TaskHubInfo> GetTaskHubInfoAsync(string taskHubName, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNullOrEmpty(taskHubName, nameof(taskHubName));

        BlobClient client = _blobServiceClient
            .GetBlobContainerClient(GetName(taskHubName))
            .GetBlobClient(TaskHubBlobName);

        try
        {
            BlobDownloadResult result = await client.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
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
