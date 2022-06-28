// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class BlobMigrationTests : IClassFixture<BlobDualWriteHttpIntegrationTestFixture<Startup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly DicomInstancesManager _instancesManager;
    private readonly bool _inprocessTestServer;

    public BlobMigrationTests(BlobDualWriteHttpIntegrationTestFixture<Startup> fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _client = fixture.GetDicomWebClient();
        _instancesManager = new DicomInstancesManager(_client);
        _inprocessTestServer = fixture.IsInProcess;
    }

    [Fact]
    public async Task WhenStoringWithDualWrite_TheServerShouldCorrectlyStoreAndRetrieve()
    {
        if (!_inprocessTestServer)
        {
            return;
        }

        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();
        VersionedInstanceIdentifier instanceIdentifier = new VersionedInstanceIdentifier(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 1);

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile });
        Assert.True(response1.IsSuccessStatusCode);

        using DicomWebResponse<DicomFile> response2 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
        Assert.True(response2.IsSuccessStatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _instancesManager.DisposeAsync();
    }
}
