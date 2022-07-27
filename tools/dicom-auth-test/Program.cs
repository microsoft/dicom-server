// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;

namespace Microsoft.Health.Web.Dicom.Tool;

public static class Program
{
    public static void Main(string[] args)
    {
        ParseArgumentsAndExecute(args);
    }

    private static void ParseArgumentsAndExecute(string[] args)
    {
        var dicomOption = new Option<string>(
            "--dicomServiceUrl",
            description: "DicomService Url ex: https://testdicomweb-testdicom.dicom.azurehealthcareapis.com");

        var rootCommand = new RootCommand("Execute Store Get and Delete of dicom image");

        rootCommand.AddOption(dicomOption);

        rootCommand.SetHandler<string>(StoreImageAsync, dicomOption);
        rootCommand.Invoke(args);
    }

    private static async Task StoreImageAsync(string dicomServiceUrl)
    {
        var dicomFile = await DicomFile.OpenAsync(@"./Image/blue-circle.dcm").ConfigureAwait(false);
        string studyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(dicomServiceUrl),
        };

        // Use VM assigned managed identity.
        var credential = new DefaultAzureCredential();

        // Access token will expire after a certain period of time.
        AccessToken token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://dicom.healthcareapis.azure.com/.default" })).ConfigureAwait(false);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        IDicomWebClient client = new DicomWebClient(httpClient);

        DicomWebResponse<DicomDataset>? response = await client.StoreAsync(dicomFile).ConfigureAwait(false);
        Console.WriteLine($"Image saved with status code: {response?.StatusCode}");

        DicomWebAsyncEnumerableResponse<DicomFile>? responseGet = await client.RetrieveStudyAsync(studyInstanceUid).ConfigureAwait(false);
        Console.WriteLine($"Image retrieved with status code: {responseGet?.StatusCode}");

        DicomWebResponse? responseDelete = await client.DeleteStudyAsync(studyInstanceUid).ConfigureAwait(false);
        Console.WriteLine($"Image deleted with status code: {responseDelete?.StatusCode}");
    }
}
