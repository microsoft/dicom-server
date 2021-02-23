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
using Common;
using Common.KeyVault;
using EnsureThat;

namespace RetrieveBlobNames
{
    public static class Program
    {
        private static string _containerConnectionString;
        private const string ContainerName = "metadatacontainer";

        public static async Task Main(string[] args)
        {
            EnsureArg.IsNotNull(args, nameof(args));

            var options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential,
                },
            };
            var client = new SecretClient(new Uri(KnownApplicationUrls.KeyVaultUrl), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret(KnownSecretNames.BlobStoreConnectionString);

            _containerConnectionString = secret.Value;

            string filepath = args[0];

            var container = new BlobContainerClient(_containerConnectionString, ContainerName);
            int i = 0;
            var studies = new HashSet<string>();
            var series = new HashSet<string>();
            using (var sw = new StreamWriter(filepath))
            {
                await foreach (BlobItem blob in container.GetBlobsAsync())
                {
                    string[] parsedInstanceName = blob.Name.Split(KnownSeparators.MessageSeparators, StringSplitOptions.RemoveEmptyEntries);
                    studies.Add(parsedInstanceName[0]);
                    series.Add(parsedInstanceName[0] + " " + parsedInstanceName[1]);
                    sw.WriteLine(blob.Name);
                    i++;
                    Console.WriteLine(blob.Name + " Count:" + i);
                }
            }

            string seriesPath = args[1];
            File.WriteAllLines(seriesPath, series);
            string studiesPath = args[2];
            File.WriteAllLines(studiesPath, studies);
        }
    }
}
