// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dicom;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Tools.ScaleTesting.Common;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.WadoMetadataFunctionApp
{
    public static class WadoMetadata
    {
        private const string TopicName = "wado-rs";
        private const string SubscriptionName = "s1";

        private const string WebServerUrl = "http://dicom-server-ii.azurewebsites.net";
        private static DicomWebClient client;

        [FunctionName("WadoMetadata")]
        public static void Run([ServiceBusTrigger(TopicName, SubscriptionName, Connection = "ServiceBusConnectionString")]byte[] message, ILogger log)
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
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(WebServerUrl);

            client = new DicomWebClient(httpClient, new Microsoft.IO.RecyclableMemoryStreamManager());
        }

        private static void RetrieveInstanceMetadata(string studyUid, string seriesUid, string instanceUid)
        {
            DicomWebResponse<IReadOnlyList<DicomDataset>> response = DicomWebClientExtensions.RetrieveInstanceMetadataAsync(client, studyUid, seriesUid, instanceUid).Result;

            return;
        }

        private static void RetrieveSeriesMetadata(string studyUid, string seriesUid)
        {
            DicomWebResponse<IReadOnlyList<DicomDataset>> response = DicomWebClientExtensions.RetrieveSeriesMetadataAsync(client, studyUid, seriesUid).Result;

            return;
        }

        private static void RetrieveStudyMetadata(string studyUid)
        {
            DicomWebResponse<IReadOnlyList<DicomDataset>> response = DicomWebClientExtensions.RetrieveStudyMetadataAsync(client, studyUid).Result;

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
