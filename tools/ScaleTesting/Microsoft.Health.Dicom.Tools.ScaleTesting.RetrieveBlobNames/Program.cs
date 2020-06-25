// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.RetrieveBlobNames
{
    public class Program
    {
        private static string _containerConnectionString;
        private const string ContainerName = "metadatacontainer";

        public static async Task Main(string[] args)
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential,
                },
            };
            var client = new SecretClient(new Uri("https://dicom-client.vault.azure.net/"), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret("ServiceBusConnectionString");

            _containerConnectionString = secret.Value;

            string filepath = args[0];
            File.Create(filepath);

            BlobContainerClient container = new BlobContainerClient(_containerConnectionString, ContainerName);
            int i = 0;
            HashSet<string> studies = new HashSet<string>();
            HashSet<(string, string)> series = new HashSet<(string, string)>();
            using (StreamWriter sw = File.AppendText(filepath))
            {
                await foreach (BlobItem blob in container.GetBlobsAsync())
                {
                    if (blob.Properties.CreatedOn.Value.Day > 17)
                    {
                        string[] parsedInstanceName = blob.Name.Split('/');
                        studies.Add(parsedInstanceName[0]);
                        series.Add((parsedInstanceName[0], parsedInstanceName[1]));
                        sw.WriteLine(blob.Name);
                        i++;
                        Console.WriteLine(blob.Name + " Count:" + i);
                    }
                }
            }

            string seriesPath = args[1];
            string studiesPath = args[2];
        }
    }
}
