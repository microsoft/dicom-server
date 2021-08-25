// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Dicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Export
{
    public class ExportService : IExportService
    {
        private readonly IRetrieveResourceService _retrieveResourceService;
        private readonly ILogger<ExportService> _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private const string DefaultAcceptType = KnownContentTypes.ApplicationDicom;

        public ExportService(IRetrieveResourceService retrieveResourceService, ILogger<ExportService> logger, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            _retrieveResourceService = retrieveResourceService;
            _logger = logger;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task Export(
            IReadOnlyCollection<string> instances,
            string destinationBlobConnectionString,
            string destinationBlobContainerName,
            string contentType = DefaultAcceptType,
            CancellationToken cancellationToken = default)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(destinationBlobConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(destinationBlobContainerName);

#pragma warning disable CA1062 // Validate arguments of public methods
            foreach (string instance in instances)
#pragma warning restore CA1062 // Validate arguments of public methods
            {
                Stream destinationStream = null;
                try
                {
                    string[] uids = instance.Split('/');
                    string studyInstanceUid = uids[0];
                    string seriesInstanceUid = uids[1];
                    string sopInstanceUid = uids[2];
                    var acceptedHeader = new AcceptHeader(KnownContentTypes.ApplicationDicom, PayloadTypes.SinglePart, "*", null);
                    RetrieveResourceRequest retrieve = new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Enumerable.Repeat<AcceptHeader>(acceptedHeader, 1));

                    var response = await _retrieveResourceService.GetInstanceResourceAsync(retrieve, cancellationToken);
                    destinationStream = response.ResponseStreams.First();
                    var fileName = GetFileName(string.Empty, studyInstanceUid, seriesInstanceUid, sopInstanceUid, "dcm");

                    await AnonymizeOneFileAsync(destinationStream, fileName, containerClient);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to export instance {instance}, error: {e}");
                }
                finally
                {
#pragma warning disable CA2012 // Use ValueTasks correctly
                    destinationStream?.DisposeAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly
                }
            }
        }

#pragma warning disable CA1822 // Mark members as static
        private string GetFileName(
#pragma warning restore CA1822 // Mark members as static
            string label,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string extension)
        {
            string folder = string.IsNullOrWhiteSpace(label) ? string.Empty : label + "/";
            return $"{folder}{studyInstanceUid}-{seriesInstanceUid}-{sopInstanceUid}.{extension}";
        }

#pragma warning disable CA1822 // Mark members as static
        private async Task SaveAsync(
#pragma warning restore CA1822 // Mark members as static
            string blobName,
            Stream imageStream,
            BlobContainerClient containerClient,
            string contentType,
            CancellationToken cancellationToken)
        {
            var blobClient = containerClient.GetBlockBlobClient(blobName);

            imageStream.Seek(0, SeekOrigin.Begin);

            await blobClient.UploadAsync(
                    imageStream,
                    new BlobHttpHeaders()
                    {
                        ContentType = contentType,
                    },
                    metadata: null,
                    conditions: null,
                    accessTier: null,
                    progressHandler: null,
                    cancellationToken);
        }

#pragma warning disable CA1822 // Mark members as static
        private async Task AnonymizeOneFileAsync(Stream stream, string fileName, BlobContainerClient containerClient)
#pragma warning restore CA1822 // Mark members as static
        {
            var engine = new AnonymizerEngine(
                  "configuration.json",
                  new AnonymizerEngineOptions());

            DicomFile dicomFile = await DicomFile.OpenAsync(stream).ConfigureAwait(false);
            engine.AnonymizeDataset(dicomFile.Dataset);
            MemoryStream ms = _recyclableMemoryStreamManager.GetStream();
            await dicomFile.SaveAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            await SaveAsync(fileName, ms, containerClient, DefaultAcceptType, CancellationToken.None);
        }
    }
}
