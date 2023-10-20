// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Extensions;
using Microsoft.Health.Operations;
using Xunit;
using FunctionsStartup = Microsoft.Health.Dicom.Functions.App.Startup;
using WebStartup = Microsoft.Health.Dicom.Web.Startup;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class ExportTests : IClassFixture<WebJobsIntegrationTestFixture<WebStartup, FunctionsStartup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly DicomInstancesManager _instanceManager;

    private const string ExpectedPathPattern = "{0}/results/{1}/{2}/{3}.dcm";

    public ExportTests(WebJobsIntegrationTestFixture<WebStartup, FunctionsStartup> fixture)
    {
        _client = EnsureArg.IsNotNull(fixture, nameof(fixture)).GetDicomWebClient();
        _instanceManager = new DicomInstancesManager(_client);
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenFiles_WhenExporting_ThenSuccessfullyCopy()
    {
        // Define DICOM files
        string studyUid1 = TestUidGenerator.Generate();
        DicomDataset instance1 = Samples.CreateRandomInstanceDataset(studyUid1);
        DicomDataset instance2 = Samples.CreateRandomInstanceDataset(studyUid1);
        DicomDataset instance3 = Samples.CreateRandomInstanceDataset(studyUid1);

        string studyUid2 = TestUidGenerator.Generate();
        string seriesUid2 = TestUidGenerator.Generate();
        DicomDataset instance4 = Samples.CreateRandomInstanceDataset(studyUid2, seriesUid2);
        DicomDataset instance5 = Samples.CreateRandomInstanceDataset(studyUid2, seriesUid2);
        DicomDataset instance6 = Samples.CreateRandomInstanceDataset(studyUid2, seriesUid2);

        string studyUid3 = TestUidGenerator.Generate();
        string seriesUid3 = TestUidGenerator.Generate();
        DicomDataset instance7 = Samples.CreateRandomInstanceDataset();
        DicomDataset instance8 = Samples.CreateRandomInstanceDataset(studyUid3);
        DicomDataset instance9 = Samples.CreateRandomInstanceDataset(studyUid3, seriesUid3);

        // Unknown DICOM instances
        string unknownStudyUid1 = TestUidGenerator.Generate();
        string unknownStudyUid2 = TestUidGenerator.Generate();
        string unknownStudyUid3 = TestUidGenerator.Generate();

        string unknownSeriesUid1 = TestUidGenerator.Generate();
        string unknownSeriesUid2 = TestUidGenerator.Generate();

        string unknownSopInstanceUid1 = TestUidGenerator.Generate();

        var instances = new Dictionary<DicomIdentifier, DicomDataset>
        {
            { DicomIdentifier.ForInstance(instance1), instance1 },
            { DicomIdentifier.ForInstance(instance2), instance2 },
            { DicomIdentifier.ForInstance(instance3), instance3 },
            { DicomIdentifier.ForInstance(instance4), instance4 },
            { DicomIdentifier.ForInstance(instance5), instance5 },
            { DicomIdentifier.ForInstance(instance6), instance6 },
            { DicomIdentifier.ForInstance(instance7), instance7 },
            { DicomIdentifier.ForInstance(instance8), instance8 },
            { DicomIdentifier.ForInstance(instance9), instance9 },
        };

        // Upload files
        await Task.WhenAll(instances.Select(x => _instanceManager.StoreAsync(new DicomFile(x.Value))));

        // Begin Export
        ExportTestOptions options = GetExportTestOptions(TestEnvironment.Variables);
        BlobContainerClient containerClient = options.CreateTestContainerClient();
        await using IAsyncDisposable scope = await ContainerScope.CreateAsync(containerClient);

        DicomWebResponse<DicomOperationReference> response = await _client.StartExportAsync(
            ExportSource.ForIdentifiers(
                DicomIdentifier.ForStudy(studyUid1),
                DicomIdentifier.ForSeries(studyUid2, seriesUid2),
                DicomIdentifier.ForInstance(instance7),
                DicomIdentifier.ForInstance(unknownStudyUid1, unknownSeriesUid1, unknownSopInstanceUid1),
                DicomIdentifier.ForSeries(unknownStudyUid2, unknownSeriesUid2),
                DicomIdentifier.ForInstance(instance8),
                DicomIdentifier.ForInstance(instance9),
                DicomIdentifier.ForStudy(unknownStudyUid3)),
            options.Destination);

        // Wait for the operation to complete
        DicomOperationReference operation = await response.GetValueAsync();
        IOperationState<DicomOperation> state = await _client.WaitForCompletionAsync(operation.Id);
        Assert.Equal(OperationStatus.Succeeded, state.Status);

        string expectedErrorLog = $"{operation.Id.ToString(OperationId.FormatSpecifier)}/errors.log";
        var results = state.Results as ExportResults;
        Assert.NotNull(results);
        Assert.EndsWith(expectedErrorLog, results.ErrorHref.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal(instances.Count, results.Exported);
        Assert.Equal(3, results.Skipped);

        // Validate the export by querying the blob container
        List<BlobItem> actual = await containerClient
            .GetBlobsAsync(prefix: operation.Id.ToString(OperationId.FormatSpecifier))
            .Where(x => x.Name.EndsWith(".dcm"))
            .ToListAsync();

        Assert.Equal(instances.Count, actual.Count);
        foreach (BlobItem blob in actual)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blob.Name);
            using Stream data = await blobClient.OpenReadAsync();
            DicomFile file = await GetDicomFileAsync(data);

            DicomIdentifier identifier = DicomIdentifier.ForInstance(file.Dataset);
            Assert.True(instances.TryGetValue(identifier, out DicomDataset expected));
            Assert.Equal(GetExpectedPath(operation.Id, identifier), blob.Name);

            await AssertEqualBinaryAsync(expected, data);
            instances.Remove(identifier);
        }

        // Check for errors
        BlobClient errorBlobClient = containerClient.GetBlobClient(expectedErrorLog);
        using var reader = new StreamReader(await errorBlobClient.OpenReadAsync());

        var errors = new List<JsonElement>();

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            errors.Add(JsonSerializer.Deserialize<JsonElement>(line));
        }

        Assert.True(errors.Count >= 3); // Duplicate scheduling may append error multiple times
        Assert.Contains(errors, e => e.GetProperty("identifier").GetString() == $"{unknownStudyUid1}/{unknownSeriesUid1}/{unknownSopInstanceUid1}");
        Assert.Contains(errors, e => e.GetProperty("identifier").GetString() == $"{unknownStudyUid2}/{unknownSeriesUid2}");
        Assert.Contains(errors, e => e.GetProperty("identifier").GetString() == $"{unknownStudyUid3}");

        // Make sure there aren't any unknown identifiers!
        Assert.All(
            errors.Select(e => e.GetProperty("identifier").GetString()),
            id => Assert.True(
                id == $"{unknownStudyUid1}/{unknownSeriesUid1}/{unknownSopInstanceUid1}" ||
                id == $"{unknownStudyUid2}/{unknownSeriesUid2}" ||
                id == $"{unknownStudyUid3}"));
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task DisposeAsync()
        => _instanceManager.DisposeAsync().AsTask();

    private static async Task AssertEqualBinaryAsync(DicomDataset expected, Stream actual)
    {
        using var buffer = new MemoryStream();
        await new DicomFile(expected).SaveAsync(buffer);
        buffer.Seek(0, SeekOrigin.Begin);
        actual.Seek(0, SeekOrigin.Begin);

        Assert.Equal(buffer, actual, BinaryComparer.Instance);
    }

    private static async Task<DicomFile> GetDicomFileAsync(Stream stream)
    {
        // DicomFile requires that the stream be seekable
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        buffer.Position = 0;

        return await DicomFile.OpenAsync(buffer);
    }

    private static string GetExpectedPath(Guid operationId, DicomIdentifier identifer)
        => string.Format(
            CultureInfo.InvariantCulture,
            ExpectedPathPattern,
            operationId.ToString(OperationId.FormatSpecifier),
            identifer.StudyInstanceUid,
            identifer.SeriesInstanceUid,
            identifer.SopInstanceUid);

    private static ExportTestOptions GetExportTestOptions(IConfiguration configuration)
    {
        var options = new ExportTestOptions();
        configuration.GetSection("Tests:Export").Bind(options);

        return options;
    }

    private sealed class ExportTestOptions : AzureBlobConnectionOptions
    {
        // Depending on the test setup, the "Sink" (i.e. the Azure Storage connection) may
        // look different from that used by the test, so optionally it can be overridden.
        // For example, this may be the case if the web app is running via docker-compose with Azurite.
        public AzureBlobConnectionOptions Sink { get; set; }

        public string BlobContainerName { get; } = Guid.NewGuid().ToString("D");

        // The BlobContainerUri is always based on the export test's connection
        private Uri BlobContainerUri => BlobServiceUri == null ? null : new Uri(BlobServiceUri, BlobContainerName);

        public ExportDataOptions<ExportDestinationType> Destination
        {
            get
            {
                AzureBlobConnectionOptions options = Sink ?? this;
                return options.BlobServiceUri != null
                    ? ExportDestination.ForAzureBlobStorage(BlobContainerUri, options.UseManagedIdentity)
                    : ExportDestination.ForAzureBlobStorage(options.ConnectionString, BlobContainerName);
            }
        }

        public BlobContainerClient CreateTestContainerClient()
        {
            if (BlobServiceUri != null)
            {
                return UseManagedIdentity
                    ? new BlobContainerClient(BlobContainerUri, new DefaultAzureCredential())
                    : new BlobContainerClient(BlobContainerUri);
            }

            return new BlobContainerClient(ConnectionString, BlobContainerName);
        }
    }

    private class AzureBlobConnectionOptions
    {
        public Uri BlobServiceUri { get; set; }

        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

        public bool UseManagedIdentity { get; set; }
    }

    private sealed class ContainerScope : IAsyncDisposable
    {
        private readonly BlobContainerClient _client;

        private ContainerScope(BlobContainerClient client)
            => _client = client;

        public static async Task<IAsyncDisposable> CreateAsync(BlobContainerClient client)
        {
            await client.CreateIfNotExistsAsync();
            return new ContainerScope(client);
        }

        public ValueTask DisposeAsync()
            => new ValueTask(_client.DeleteIfExistsAsync());
    }
}
