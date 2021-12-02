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
using System.Security.Cryptography.Xml;

namespace Microsoft.Health.DicomCast.Hosting
{
    public class FhirResourceToBlobHackService : BackgroundService
    {
        private readonly IFhirClient _fhirClient;
        private readonly FhirConfiguration _fhirConfiguration;
        private readonly ILogger<FhirResourceToBlobHackService> _logger;
        private readonly HashSet<string> _processedResources;

        private string _currentContinuationToken;


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


        // one time job
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // patients
                    int cnt = 0;
                    do
                    {
                        var patients = await FindAll<HL7M.Patient>(GetCtString(), stoppingToken);
                        cnt = 0;
                        foreach (var patient in patients)
                        {
                            if (!_processedResources.Contains(patient.Key))
                            {
                                await UploadToBlob(_fhirConfiguration.BlobEndpoint, "fhirpatient", patient.Key, patient.Value, stoppingToken);
                                _processedResources.Add(patient.Key);
                                _logger.LogInformation($"Patients {patient.Key} processed.");
                                cnt++;
                            }
                        }
                        _logger.LogInformation($"token {_currentContinuationToken}");
                    }
                    while (_currentContinuationToken != null && cnt == 1000);

                    // Consent
                    do
                    {
                        var consents = await FindAll<HL7M.Consent>(GetCtString(), stoppingToken);
                        cnt = 0;
                        foreach (var consent in consents)
                        {
                            if (!_processedResources.Contains(consent.Key))
                            {
                                await UploadToBlob(_fhirConfiguration.BlobEndpoint, "fhirconsent", consent.Key, consent.Value, stoppingToken);
                                _processedResources.Add(consent.Key);
                                _logger.LogInformation($"consents {consent.Key} processed.");
                                cnt++;
                            }
                        }
                    } while (_currentContinuationToken != null && cnt == 1000);

                    // Diagnostic reports
                    do
                    {
                        var diags = await FindAll<HL7M.DiagnosticReport>(GetCtString(), stoppingToken);
                        cnt = 0;
                        foreach (var diag in diags)
                        {
                            if (!_processedResources.Contains(diag.Key))
                            {
                                await UploadToBlob(_fhirConfiguration.BlobEndpoint, "fhirdiagnosticreport", diag.Key, diag.Value, stoppingToken);
                                _processedResources.Add(diag.Key);
                                _logger.LogInformation($"DiagnosticReport {diag.Key} processed.");
                                cnt++;
                            }
                        }
                        _logger.LogInformation($"token {_currentContinuationToken}");
                    }
                    while (_currentContinuationToken != null && cnt == 1000);

                    //// ImagingStudy
                    do
                    {
                        var studies = await FindAll<HL7M.ImagingStudy>(GetCtString(), stoppingToken);
                        cnt = 0;
                        foreach (var study in studies)
                        {
                            if (!_processedResources.Contains(study.Key))
                            {
                                await UploadToBlob(_fhirConfiguration.BlobEndpoint, "fhirimagingstudy", study.Key, study.Value, stoppingToken);
                                _processedResources.Add(study.Key);

                                _logger.LogInformation($"fhirimagingstudy {study.Key} processed.");
                                cnt++;
                            }
                        }
                    } while (_currentContinuationToken != null && cnt == 1000);

                    await Task.Delay(10000, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "failed");
                    break;
                }
            }
        }


        private string GetCtString()
        {
            //if (_currentContinuationToken != null)
            //{
            //    return $"ct={_currentContinuationToken}";
            //}
            return _currentContinuationToken;
        }

        private async Task<Dictionary<string, string>> FindAll<TResource>(string query, CancellationToken cancellationToken)
           where TResource : HL7M.Resource, new()
        {
            string fhirTypeName = HL7M.ModelInfo.GetFhirTypeNameForType(typeof(TResource));
            if (!Enum.TryParse(fhirTypeName, out HL7M.ResourceType resourceType))
            {
                Debug.Assert(false, "Resource type could not be parsed from TResource");
            }

            HL7M.Bundle bundle = await _fhirClient.SearchAsync(
                resourceType,
                query: query,
                count: 1000,
                cancellationToken);

            if (bundle.NextLink != null)
            {
                var parsedString = bundle.NextLink.AbsoluteUri.Split('?')[1].Split('&');
                if (parsedString[0].Contains("ct", StringComparison.OrdinalIgnoreCase))
                {
                    _currentContinuationToken = parsedString[0];
                }
                else
                {
                    _currentContinuationToken = parsedString[1];
                }
            }
            else
            {
                _currentContinuationToken = null;
            }

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
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString.ToString());
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
