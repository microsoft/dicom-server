// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tools.ScaleTesting.Common;
using Microsoft.Health.Dicom.Tools.ScaleTesting.Common.ServiceBus;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.QidoFunctionApp
{
    public static class Qido
    {
        private const string WebServerUrl = "http://dicom-server-ii.azurewebsites.net";
        private static DicomWebClient client;

        [FunctionName("Qido")]
        public static void Run([ServiceBusTrigger(KnownTopics.Qido, KnownSubscriptions.S1, Connection = "ServiceBusConnectionString")]byte[] message, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {Encoding.UTF8.GetString(message)}");
            SetupDicomWebClient();

            try
            {
                ProcessMessageWithQueryUrl(message, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static void ProcessMessageWithQueryUrl(byte[] message, ILogger log)
        {
            DicomWebResponse<string> response = client.QueryWithBadRequest(Encoding.UTF8.GetString(message)).Result;
        }

        private static void SetupDicomWebClient()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(WebServerUrl),
            };

            client = new DicomWebClient(httpClient);
        }
    }
}
