// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;

namespace Microsoft.Health.Dicom.FileUploader;

public static class Program
{
    public static void Main(string[] args)
    {
        ParseArgumentsAndExecute(args);
    }

    private static void ParseArgumentsAndExecute(string[] args)
    {
        var dicomOption = new Option<Uri>(
            "--dicomServiceUrl",
            description: "Dicom service URL, for example: https://testdicomweb-testdicom.dicom.azurehealthcareapis.com",
            getDefaultValue: () => new Uri("https://localhost:63838"));

        var filePath = new Option<string>(
            "--path",
            description: "Path to a directory containing .dcm files to be uploaded.",
            getDefaultValue: () => @"./Images");

        var deleteFiles = new Option<bool>(
            "--deleteFiles",
            description: "Delete files after a successful uploaded.");

        var rootCommand = new RootCommand("Upload DICOM image(s)");

        rootCommand.AddOption(dicomOption);
        rootCommand.AddOption(filePath);
        rootCommand.AddOption(deleteFiles);

        rootCommand.SetHandler(StoreImageAsync, dicomOption, filePath, deleteFiles);
        rootCommand.Invoke(args);
    }

    private static async Task StoreImageAsync(Uri dicomServiceUrl, string filePath, bool deleteFiles)
    {
        var files = new List<string> { @"./Image/blue-circle.dcm" };

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            files = Directory.GetFiles(filePath, "*.dcm").ToList();
        }

        using var httpClient = new HttpClient();

        httpClient.BaseAddress = dicomServiceUrl;

        // Use locally present identity, which would be managed identity on a VM in Azure.
        var credential = new DefaultAzureCredential();

        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://dicom.healthcareapis.azure.com/.default" }));
        var accessToken = token.Token;

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        IDicomWebClient client = new DicomWebClient(httpClient);

        var stopwatch = Stopwatch.StartNew();
        foreach (string file in files)
        {
            var dicomFile = await DicomFile.OpenAsync(file);
            var response = await client.StoreAsync(dicomFile);
            Console.WriteLine($"{dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)}/{dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID)}/{dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)} saved with status code: {response.StatusCode}");

            if (deleteFiles)
            {
                File.Delete(file);
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"{files.Count} files uploaded from {filePath} to {dicomServiceUrl} in {stopwatch.Elapsed}");
    }
}
