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

namespace QidoFunctionApp
{
    public static class Qido
    {
        private static IDicomWebClient s_client;

        [FunctionName("Qido")]
        public static void Run([ServiceBusTrigger(KnownTopics.Qido, KnownSubscriptions.S1, Connection = "ServiceBusConnectionString")] byte[] message, ILogger log)
        {
            var url = new Uri(Encoding.UTF8.GetString(message));
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {url}");
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(KnownApplicationUrls.DicomServerUrl),
            };

            SetupDicomWebClient(httpClient);

            try
            {
                ProcessMessageWithQueryUrl(url);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static void ProcessMessageWithQueryUrl(Uri url)
        {
            s_client.QueryAsync(url).Wait();
        }

        private static void SetupDicomWebClient(HttpClient httpClient)
        {
            s_client = new DicomWebClient(httpClient);
        }
    }
}
