// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Health.Dicom.Client;

namespace DicomUploaderFunction;

public class BlobUploader
{
    private readonly IDicomWebClient _dicomWebClient;

    public BlobUploader(IDicomWebClient dicomWebClient)
    {
        _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
    }

    [FunctionName("BlobUploader")]
    public async Task Run(
        [BlobTrigger("%sourcestorage:blobcontainer%/{name}", Connection = "sourcestorage")]
        Stream myBlob,
        CancellationToken cancellationToken)
    {
        using var streamContent = new StreamContent(myBlob);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/dicom");
        await _dicomWebClient.StoreAsync(streamContent, partitionName: null, cancellationToken);
    }
}
