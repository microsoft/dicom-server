// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Dicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Anonymizer.Core;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Core.Features.Cohort;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Export
{
    public class ExportService : IExportService
    {
        private readonly IRetrieveResourceService _retrieveResourceService;
        private readonly ILogger<ExportService> _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private const string DefaultAcceptType = KnownContentTypes.ApplicationDicom;
        private readonly ICohortQueryStore _cohortQueryStore;

        public ExportService(ICohortQueryStore cohortQueryStore, IRetrieveResourceService retrieveResourceService, ILogger<ExportService> logger, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            _cohortQueryStore = cohortQueryStore;
            _retrieveResourceService = retrieveResourceService;
            _logger = logger;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task Export(
            IReadOnlyCollection<string> instances,
            string cohortId,
            string destinationBlobConnectionString,
            string destinationBlobContainerName,
            string contentType = DefaultAcceptType,
            CancellationToken cancellationToken = default)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(destinationBlobConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(destinationBlobContainerName);

            try
            {
                if (!string.IsNullOrEmpty(cohortId))
                {
                    (List<string> fhirInstances, List<string> dicomInstances) = await GetInstanceIdsFromCohort(cohortId);

                    // Export and deanon DICOM
                    await ExportAndAnonDicomDataAsync(dicomInstances, containerClient);

                    await GetAndUploadFHIRAsync(fhirInstances, containerClient);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private async Task GetAndUploadFHIRAsync(List<string> fhirInstances, BlobContainerClient containerClient)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            using var client = new HttpClient();
            // fhirInstances = Enumerable.Repeat<string>("https://sjbdcast6-fhir.azurewebsites.net/Patient/e04a7f3d-6621-4da2-bbb6-6ffc0b9bdbc8", 1).ToList();
#pragma warning disable CA1062 // Validate arguments of public methods
            MemoryStream output = null;
            foreach (var instance in fhirInstances)
#pragma warning restore CA1062 // Validate arguments of public methods
            {
                using var httpResponse = await client.GetAsync(new Uri(instance), HttpCompletionOption.ResponseHeadersRead);
                httpResponse.EnsureSuccessStatusCode();

                try
                {
                    var ms = await httpResponse.Content.ReadAsStreamAsync();
                    // https://sjbdcast6-fhir.azurewebsites.net/Patient/e04a7f3d-6621-4da2-bbb6-6ffc0b9bdbc8
#pragma warning disable CA1310 // Specify StringComparison for correctness
                    var fileName = "FHIR - " + instance.Substring(instance.IndexOf("Patient/") + 8) + ".json";
#pragma warning restore CA1310 // Specify StringComparison for correctness

#pragma warning disable CA2000 // Dispose objects before losing scope
                    StreamReader reader = new StreamReader(ms);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    string resourceJson = reader.ReadToEnd();
                    try
                    {
                        var engine = Fhir.Anonymizer.Core.AnonymizerEngine.CreateWithFileContext("configuration-R4-version.json");
                        var settings = new AnonymizerSettings()
                        {
                            IsPrettyOutput = true,
                            ValidateInput = false,
                            ValidateOutput = false
                        };
                        var resourceResult = engine.AnonymizeJson(resourceJson, settings);
                        byte[] byteArray = Encoding.ASCII.GetBytes(resourceResult);
#pragma warning disable CA2000 // Dispose objects before losing scope
                        output = new MemoryStream(byteArray);
#pragma warning restore CA2000 // Dispose objects before losing scope
                        await SaveAsync(fileName, output, containerClient, DefaultAcceptType, CancellationToken.None);
                    }
                    catch (Exception innerException)
                    {
                        Console.Error.WriteLine($"[{fileName}] Error:\nResource: {resourceJson}\nErrorMessage: {innerException.ToString()}");
                        throw;
                    }
                    finally
                    {
#pragma warning disable CA2012 // Use ValueTasks correctly
                        output?.DisposeAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task ExportAndAnonDicomDataAsync(List<string> instances, BlobContainerClient containerClient)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            foreach (string instance in instances)
#pragma warning restore CA1062 // Validate arguments of public methods
            {
                Stream destinationStream = null;
                try
                {
                    string[] uids = instance.Split('-');
                    string studyInstanceUid = uids[0];
                    string seriesInstanceUid = uids[1];
                    string sopInstanceUid = uids[2];
                    var acceptedHeader = new AcceptHeader(KnownContentTypes.ApplicationDicom, PayloadTypes.SinglePart, "*", null);
                    RetrieveResourceRequest retrieve = new RetrieveResourceRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Enumerable.Repeat<AcceptHeader>(acceptedHeader, 1));

                    var response = await _retrieveResourceService.GetInstanceResourceAsync(retrieve, CancellationToken.None);
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

        private async Task<(List<string>, List<string>)> GetInstanceIdsFromCohort(string cohortId)
        {
            var cohorts = await _cohortQueryStore.GetCohortResources(Guid.Parse(cohortId), CancellationToken.None);
            var dicomInstances = cohorts.CohortResources.Where(x => x.ResourceType == Models.CohortResourceType.DICOM).Select(x => x.ResourceId).ToList();
            var fhirInstances = cohorts.CohortResources.Where(x => x.ResourceType == Models.CohortResourceType.FHIR).Select(x => x.ReferenceUrl).ToList();
            return (fhirInstances, dicomInstances);
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
            return $"DICOM - {folder}{studyInstanceUid}-{seriesInstanceUid}-{sopInstanceUid}.{extension}";
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

            // imageStream.Seek(0, SeekOrigin.Begin);

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
