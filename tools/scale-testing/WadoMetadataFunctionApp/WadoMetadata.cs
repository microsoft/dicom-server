// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Common;
using Common.ServiceBus;
using Dicom;
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
            SetupDicomWebClient();

            try
            {
                ProcessMessage(message, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static void SetupDicomWebClient()
        {
            Uri baseAddress = new Uri(KnownApplicationUrls.DicomServerUrl);

            client = new DicomWebClient(baseAddress, new HttpClientHandler());
        }

        private static void RetrieveInstanceMetadata(string studyUid, string seriesUid, string instanceUid)
        {
            DicomWebAsyncEnumerableResponse<DicomDataset> response = DicomWebClientExtensions.RetrieveInstanceMetadataAsync(client, studyUid, seriesUid, instanceUid).Result;

            return;
        }

        private static void RetrieveSeriesMetadata(string studyUid, string seriesUid)
        {
            DicomWebAsyncEnumerableResponse<DicomDataset> response = DicomWebClientExtensions.RetrieveSeriesMetadataAsync(client, studyUid, seriesUid).Result;

            return;
        }

        private static void RetrieveStudyMetadata(string studyUid)
        {
            DicomWebAsyncEnumerableResponse<DicomDataset> response = DicomWebClientExtensions.RetrieveStudyMetadataAsync(client, studyUid).Result;

            return;
        }

        private static void ProcessMessage(byte[] message, ILogger log)
        {
            string messageBody = Encoding.UTF8.GetString(message);
            string[] split = messageBody.Split(new string[] { "\t", "  " }, StringSplitOptions.RemoveEmptyEntries);

            if (split.Count() == 1)
            {
                RetrieveStudyMetadata(split[0]);
            }
            else if (split.Count() == 2)
            {
                RetrieveSeriesMetadata(split[0], split[1]);
            }
            else if (split.Count() == 3)
            {
                RetrieveInstanceMetadata(split[0], split[1], split[2]);
            }

            log.LogInformation("Successfully retrieved file.");
        }
    }
}
