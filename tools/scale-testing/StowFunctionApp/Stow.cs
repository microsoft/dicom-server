// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Common;
using Common.ServiceBus;
using Dicom;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client;

namespace StowFunctionApp
{
    public static class Stow
    {
        private static IDicomWebClient client;

        [FunctionName("StorePreGeneratedData")]
        public static void Run([ServiceBusTrigger(KnownTopics.StowRs, KnownSubscriptions.S1, Connection = "ServiceBusConnectionString")]byte[] message, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {Encoding.UTF8.GetString(message)}");

            SetupDicomWebClient();

            try
            {
                // ProcessMessageWithInstanceReference(message, log);
                // ProcessMessageWithStudyReference(message, log);
                ProcessMessageWithPreGeneratedData(message);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }

        private static void SetupDicomWebClient()
        {
            Uri baseAddress = new Uri(KnownApplicationUrls.DicomServerUrl);

            client = new DicomWebClient(baseAddress);
        }

        private static void StoreRetrievedData(DicomFile dicomFile)
        {
            DicomWebResponse<DicomDataset> response = client.StoreAsync(new List<DicomFile>() { dicomFile }).Result;

            int statusCode = (int)response.StatusCode;
            if (statusCode != 409 && statusCode < 200 && statusCode > 299)
            {
                throw new Exception();
            }

            return;
        }

        /* private static void ProcessMessageWithInstanceReference(byte[] message, ILogger log)
        {
            // Get a reference to a blob named "sample-file"
            CloudBlob blob = container.GetBlobReference(Encoding.UTF8.GetString(message));

            // Read Blob
            Stream stream = blob.OpenRead();
            DicomFile dicomFile = DicomFile.Open(stream);

            System.Diagnostics.Trace.WriteLine(ToInstanceIdentifier(dicomFile.Dataset).ToString());
            log.LogInformation(ToInstanceIdentifier(dicomFile.Dataset).ToString());

            StoreRetrievedData(dicomFile);

            log.LogInformation("Successfully stored file.");
        }

        private static void ProcessMessageWithStudyReference(byte[] message, ILogger log)
        {
            string studyBlobs = Encoding.UTF8.GetString(message);

            string[] instances = studyBlobs.Split(',');

            foreach (string instance in instances)
            {
                // Get a reference to a blob named "sample-file"
                CloudBlob blob = container.GetBlobReference(instance);

                // Read Blob
                Stream stream = blob.OpenRead();
                DicomFile dicomFile = DicomFile.Open(stream);

                System.Diagnostics.Trace.WriteLine(ToInstanceIdentifier(dicomFile.Dataset).ToString());
                log.LogInformation(ToInstanceIdentifier(dicomFile.Dataset).ToString());

                // StoreRetrievedData(dicomFile);

                log.LogInformation("Successfully stored file.");
            }
        } */

        private static void ProcessMessageWithPreGeneratedData(byte[] message)
        {
            PatientInstance pI = JsonSerializer.Deserialize<PatientInstance>(Encoding.UTF8.GetString(message));

            // 400, 400, 100 - 16MB
            // 100, 100, 100 - 1MB
            // 100, 100, 50 - 0.5MB
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(
                        pI,
                        rows: 100,
                        columns: 100,
                        frames: 50);
            StoreRetrievedData(dicomFile);
        }

        private static InstanceIdentifier ToInstanceIdentifier(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new InstanceIdentifier(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty));
        }
    }
}
