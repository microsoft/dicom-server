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

namespace WadoFunctionApp
{
    public static class Wado
    {
        private static IDicomWebClient client;

        [FunctionName("WadoInstance")]
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
                ProcessMessageWithInstanceReference(message, log);
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

        private static void RetrieveInstance(string studyUid, string seriesUid, string instanceUid)
        {
            client.RetrieveInstanceAsync(studyUid, seriesUid, instanceUid).Wait();
        }

        private static void ProcessMessageWithInstanceReference(byte[] message, ILogger log)
        {
            string messageBody = Encoding.UTF8.GetString(message);
            string[] split = messageBody.Split(new string[] { "\t", "  " }, StringSplitOptions.RemoveEmptyEntries);

            System.Diagnostics.Trace.WriteLine(ToInstanceIdentifier(split[0], split[1], split[2]).ToString());
            log.LogInformation(ToInstanceIdentifier(split[0], split[1], split[2]).ToString());

            RetrieveInstance(split[0], split[1], split[2]);

            log.LogInformation("Successfully retrieved file.");
        }

        private static InstanceIdentifier ToInstanceIdentifier(string studyUid, string seriesUid, string instanceUid)
        {
            return new InstanceIdentifier(
                studyUid,
                seriesUid,
                instanceUid);
        }
    }
}
