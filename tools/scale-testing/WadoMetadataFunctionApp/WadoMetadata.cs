// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text;
using Common;
using Common.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client;

namespace WadoMetadataFunctionApp
{
    public static class WadoMetadata
    {
        private static IDicomWebClient client;

        [FunctionName("WadoMetadata")]
        public static void Run([ServiceBusTrigger(KnownTopics.WadoRs, KnownSubscriptions.S1, Connection = "ServiceBusConnectionString")]byte[] message, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {message}");
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(KnownApplicationUrls.DicomServerUrl),
            };

            SetupDicomWebClient(httpClient);

            try
            {
                ProcessMessage(message, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static void SetupDicomWebClient(HttpClient httpClient)
        {
            client = new DicomWebClient(httpClient);
        }

        private static void RetrieveInstanceMetadata(string studyUid, string seriesUid, string instanceUid)
        {
            DicomWebClientExtensions.RetrieveInstanceMetadataAsync(client, studyUid, seriesUid, instanceUid).Wait();
        }

        private static void RetrieveSeriesMetadata(string studyUid, string seriesUid)
        {
            DicomWebClientExtensions.RetrieveSeriesMetadataAsync(client, studyUid, seriesUid).Wait();
        }

        private static void RetrieveStudyMetadata(string studyUid)
        {
            DicomWebClientExtensions.RetrieveStudyMetadataAsync(client, studyUid).Wait();
        }

        private static void ProcessMessage(byte[] message, ILogger log)
        {
            string messageBody = Encoding.UTF8.GetString(message);
            string[] split = messageBody.Split(new string[] { "\t", "  " }, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 1)
            {
                RetrieveStudyMetadata(split[0]);
            }
            else if (split.Length == 2)
            {
                RetrieveSeriesMetadata(split[0], split[1]);
            }
            else if (split.Length == 3)
            {
                RetrieveInstanceMetadata(split[0], split[1], split[2]);
            }

            log.LogInformation("Successfully retrieved file.");
        }
    }
}
