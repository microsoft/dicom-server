// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
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

        var rootCommand = new RootCommand("Upload DICOM image(s)");

        rootCommand.AddOption(dicomOption);
        rootCommand.AddOption(filePath);

        rootCommand.SetHandler(StoreImageAsync, dicomOption, filePath);
        rootCommand.Invoke(args);
    }

    private static async Task StoreImageAsync(Uri dicomServiceUrl, string filePath)
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

        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://dicom.healthcareapis.azure.com/.default"]));
        var accessToken = token.Token;

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        DicomWebClient client = new(httpClient);

        foreach (string file in files)
        {
            DicomFile dicomFile = await DicomFile.OpenAsync(file);
            var response = await client.StoreAsync(dicomFile, dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));

            Console.WriteLine($"{dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)}/{dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID)}/{dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)} saved with status code: {response.StatusCode}");
        }
    }
}
