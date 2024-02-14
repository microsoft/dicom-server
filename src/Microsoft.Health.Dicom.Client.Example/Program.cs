// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Health.Dicom.Client;

const string TenantId = "<tenant>";
const string SourceUrl = "<source>";
const string DestinationUrl = "<destination>";
const string StudyInstanceUid = "<studyInstanceUid>";

// Fetch a token from AAD
DefaultAzureCredential credential = new();
TokenRequestContext context = new(["https://dicom.healthcareapis.azure.com/.default"], tenantId: TenantId);
AccessToken tokenResponse = await credential.GetTokenAsync(context);
AuthenticationHeaderValue value = new("Bearer", tokenResponse.Token);

// Create the HTTP clients used for communicating with the DICOM services
using HttpClient sourceClient = new()
{
    BaseAddress = new Uri(SourceUrl, UriKind.Absolute),
    DefaultRequestHeaders = { Authorization = value },
};

using HttpClient destClient = new()
{
    BaseAddress = new Uri(DestinationUrl, UriKind.Absolute),
    DefaultRequestHeaders = { Authorization = value },
};

// Create the DICOM web clients
DicomWebClient source = new(sourceClient);
DicomWebClient dest = new(destClient);

// Copy the study from the source to the destination
using DicomWebResponse response = await source.RetrieveStudyResponseAsync(StudyInstanceUid);
await dest.StoreAsync(response.Content);
