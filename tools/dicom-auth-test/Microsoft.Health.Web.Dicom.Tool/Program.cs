// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;

namespace Microsoft.Health.Web.Dicom.Tool;

public static class Programstore
{
    public static async Task Main(string[] args)
    {
        ParseArgumentsAndExecute(args);
        await StoreImageAsync();
    }

    private static void ParseArgumentsAndExecute(string[] args)
    {
        var rootCommand = new RootCommand();

        // Create a provisioning command with some options
        var dicomWebCommand = new Command("Execute", "Execute Store Get and Delete");

        dicomWebCommand.Handler = CommandHandler.Create(StoreImageAsync);

        rootCommand.AddCommand(dicomWebCommand);
        rootCommand.Invoke(args);

    }

    private static async Task StoreImageAsync()
    {
        var dicomFile = await DicomFile.OpenAsync(@"./Image/blue-circle.dcm");

        using var httpClient = new HttpClient();

        //  put the serviceurl of your provisioned provisioned dicom service.
        string dicomServiceUrl = "https://testdicomtool-dicomone.dicom.azurehealthcareapis.com"
        httpClient.BaseAddress = new Uri(dicomServiceUrl);

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://dicom.healthcareapis.azure.com/.default" }));
        var accessToken = token.Token;

        // Access token will expire after a certain period of time.
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        IDicomWebClient client = new DicomWebClient(httpClient);

        var response = await client.StoreAsync(dicomFile);

        string output = new string("Image saved with statuscode: ");
        Console.WriteLine(output + response.StatusCode);

        string studyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

        var responseGet = await client.RetrieveStudyAsync(studyInstanceUid);

        output = new string("Image retrieved with statuscode: ");
        Console.WriteLine(output + responseGet.StatusCode);

        var responseDelete = await client.DeleteStudyAsync(studyInstanceUid);

        output = new string("Image retrieved with statuscode: ");
        Console.WriteLine(output + responseDelete.StatusCode);
    }
}
