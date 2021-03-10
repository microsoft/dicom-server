// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Net.Http;
using Common;
using Dicom;
using Microsoft.Health.Dicom.Client;

namespace StowWebApp
{
    public static class Program
    {
        private static Random rand;
        private static IDicomWebClient client;

        public static async Task Main()
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(KnownApplicationUrls.DicomServerUrl),
            };

            SetupDicomWebClient(httpClient);

            // update to total count/instances of web app.
            int totalCount = 1000;

            int tracker = 0;

            while (tracker < totalCount)
            {
                    List<(string, string, string)> instances = InstanceGenerator();

                    foreach ((string studyUID, string seriesUID, string instanceUID) inst in instances)
                    {
                        // 400, 400, 100 - 16MB
                        // 100, 100, 100 - 1MB
                        // 100, 100, 50 - 0.5MB
                        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(
                                    inst.studyUID,
                                    inst.seriesUID,
                                    inst.instanceUID,
                                    rows: 100,
                                    columns: 100,
                                    frames: 50);
                        try
                        {

                            DicomWebResponse<DicomDataset> response = await client.StoreAsync(new List<DicomFile>() { dicomFile }, cancellationToken: token);

                            int statusCode = (int)response.StatusCode;
                            if (statusCode != 409 && statusCode < 200 && statusCode > 299)
                            {
                                throw new HttpRequestException("Stow operation failed", null, response.StatusCode);
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
                        }

                        tracker++;
                    }
            }
        }

        private static void SetupDicomWebClient(HttpClient httpClient)
        {
            client = new DicomWebClient(httpClient);
        }

        private static List<(string, string, string)> InstanceGenerator()
        {
            List<(string studyUID, string seriesUID, string instanceUID)> ret = new List<(string, string, string)>();
            string studyUid = DicomUID.Generate().UID;
            int series = rand.Next(1, 5);
            for (int i = 0; i < series; i++)
            {
                string seriesUid = DicomUID.Generate().UID;
                int instances = rand.Next(1, 7);
                for (int j = 0; j < instances; j++)
                {
                    string instanceUid = DicomUID.Generate().UID;
                    ret.Add((studyUid, seriesUid, instanceUid));
                }
            }

            return ret;
        }
    }
}
