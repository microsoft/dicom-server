// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HL7M = Hl7.Fhir.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Fhir.Client;
using Azure.Storage.Blobs;
using System.IO;
using System.Text;
using Hl7.Fhir.Serialization;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.DicomCast.Hosting
{
    public class FhirResourceToBlobHackService : BackgroundService
    {
        private readonly IFhirClient _fhirClient;
        private readonly FhirConfiguration _fhirConfiguration;
        private readonly ILogger<FhirResourceToBlobHackService> _logger;
        private readonly HashSet<string> _processedResources;

        public FhirResourceToBlobHackService(
            IFhirClient fhirClient,
            IOptions<FhirConfiguration> fhirConfiguration,
            ILogger<FhirResourceToBlobHackService> logger)
        {
            _fhirClient = fhirClient;
            _fhirConfiguration = fhirConfiguration?.Value;
            _logger = logger;
            _processedResources = new HashSet<string>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var patients = await FindAll<HL7M.Patient>(stoppingToken);

                    foreach (var patient in patients)
                    {
                        if (!_processedResources.Contains(patient.Key))
                        {
                            await UploadToBlob(_fhirConfiguration.BlobEndpoint, "fhirpatient", patient.Key, patient.Value, stoppingToken);
                            _processedResources.Add(patient.Key);
                        }
                    }

                    await Task.Delay(3000, stoppingToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
        }

        private async Task<Dictionary<string, string>> FindAll<TResource>(CancellationToken cancellationToken)
           where TResource : HL7M.Resource, new()
        {
            string fhirTypeName = HL7M.ModelInfo.GetFhirTypeNameForType(typeof(TResource));
            if (!Enum.TryParse(fhirTypeName, out HL7M.ResourceType resourceType))
            {
                Debug.Assert(false, "Resource type could not be parsed from TResource");
            }

            HL7M.Bundle bundle = await _fhirClient.SearchAsync(
                resourceType,
                query: null,
                count: null,
                cancellationToken);

            int matchCount = 0;
            var results = new Dictionary<string, string>();

            if (bundle != null)
            {
                matchCount += bundle.Entry.Count;

                for (int i = 0; i < matchCount; i++)
                {
                    string jsonEntry = bundle.Entry[i].ToJson();
                    string entryId = ((TResource)bundle.Entry[i].Resource).Id;
                    results.Add(entryId, jsonEntry);
                }
            }
            return results;
        }

        private static async Task UploadToBlob(Uri connectionString, string containerName, string id, string jsonContent, CancellationToken stoppingToken)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var blobClient = containerClient.GetBlobClient(id);
            var content = Encoding.UTF8.GetBytes(jsonContent);
            using (var ms = new MemoryStream(content))
            {

                await blobClient.UploadAsync(
                    ms,
                    new BlobHttpHeaders()
                    {
                        ContentType = "application/json",
                    },
                    cancellationToken: stoppingToken);
            }
        }
    }

}
