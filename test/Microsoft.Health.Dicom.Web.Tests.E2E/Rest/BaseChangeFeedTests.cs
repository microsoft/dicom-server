// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class BaseChangeFeedTests : IAsyncLifetime
{
    private readonly DicomInstancesManager _instancesManager;

    public BaseChangeFeedTests(IDicomWebClient client)
    {
        Client = client;
        _instancesManager = new DicomInstancesManager(client);
    }

    protected IDicomWebClient Client { get; }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task DisposeAsync()
        => _instancesManager.DisposeAsync().AsTask();

    protected async Task<InstanceIdentifier> CreateFileAsync(string studyInstanceUid = null, string seriesInstanceUid = null, string sopInstanceUid = null, string partitionName = null)
    {
        studyInstanceUid ??= TestUidGenerator.Generate();
        seriesInstanceUid ??= TestUidGenerator.Generate();
        sopInstanceUid ??= TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile }, studyInstanceUid, partitionName);
        DicomDataset dataset = await response.GetValueAsync();

        return new InstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
    }

    protected static string ToQueryString(
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        long? offset = null,
        int? limit = null,
        bool? includeMetadata = null)
    {
        var builder = new StringBuilder("?");

        if (startTime.HasValue)
            builder.Append(CultureInfo.InvariantCulture, $"{nameof(startTime)}={HttpUtility.UrlEncode(startTime.GetValueOrDefault().ToString("O", CultureInfo.InvariantCulture))}");

        if (endTime.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(endTime)}={HttpUtility.UrlEncode(endTime.GetValueOrDefault().ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (offset.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(offset)}={offset.GetValueOrDefault()}");
        }

        if (limit.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(limit)}={limit.GetValueOrDefault()}");
        }

        if (includeMetadata.HasValue)
        {
            if (builder.Length > 1)
                builder.Append('&');

            builder.Append(CultureInfo.InvariantCulture, $"{nameof(includeMetadata)}={includeMetadata.GetValueOrDefault()}");
        }

        return builder.ToString();
    }
}
