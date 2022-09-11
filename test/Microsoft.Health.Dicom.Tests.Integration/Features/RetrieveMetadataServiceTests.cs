// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Serialization;
using Microsoft.Health.Dicom.Tests.Integration.Persistence;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features;

public class RetrieveMetadataServiceTests : IClassFixture<DataStoreTestsFixture>
{
    private readonly RetrieveMetadataService _retrieveMetadataService;
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IETagGenerator _eTagGenerator;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;

    public RetrieveMetadataServiceTests(DataStoreTestsFixture storagefixture)
    {
        EnsureArg.IsNotNull(storagefixture, nameof(storagefixture));
        _instanceStore = Substitute.For<IInstanceStore>();
        _metadataStore = storagefixture.MetadataStore;
        _eTagGenerator = Substitute.For<IETagGenerator>();
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();

        _dicomRequestContextAccessor.RequestContext.DataPartitionEntry = PartitionEntry.Default;

        _retrieveMetadataService = new RetrieveMetadataService(_instanceStore, _metadataStore, _eTagGenerator, _dicomRequestContextAccessor);
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();
        string studyInstanceUid = TestUidGenerator.Generate();

        (_, _, DicomDataset second, int version) = SetupDatasetList(studyInstanceUid, currentVersion: 1, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for only one instance in the given list
        await _metadataStore.StoreInstanceMetadataAsync(second, version);

        await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, ifNoneMatch, tokenSource.Token));
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();
        string studyInstanceUid = TestUidGenerator.Generate();

        SetupDatasetList(studyInstanceUid, currentVersion: 10, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, ifNoneMatch, tokenSource.Token));
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudy_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
    {
        using var tokenSource = new CancellationTokenSource();
        string studyInstanceUid = TestUidGenerator.Generate();

        (DicomDataset first, int firstVersion, DicomDataset second, int secondVersion) = SetupDatasetList(studyInstanceUid, currentVersion: 100, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for all instances in the given list
        await _metadataStore.StoreInstanceMetadataAsync(first, firstVersion);
        await _metadataStore.StoreInstanceMetadataAsync(second, secondVersion);

        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, ifNoneMatch, tokenSource.Token);

        var actual = response.ResponseMetadata.ToList();
        Assert.Equal(2, actual.Count);
        ValidateResponseMetadataDataset(first, actual[0]);
        ValidateResponseMetadataDataset(second, actual[1]);
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        (_, _, DicomDataset second, int version) = SetupDatasetList(studyInstanceUid, seriesInstanceUid, currentVersion: 1000, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for only one instance in the given list
        await _metadataStore.StoreInstanceMetadataAsync(second, version);

        await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch, tokenSource.Token));
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        SetupDatasetList(studyInstanceUid, seriesInstanceUid, currentVersion: 10000, cancellationToken: tokenSource.Token);

        string ifNoneMatch = null;
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch, tokenSource.Token));
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForSeries_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
    {
        using var tokenSource = new CancellationTokenSource();
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        (DicomDataset first, int firstVersion, DicomDataset second, int secondVersion) = SetupDatasetList(studyInstanceUid, seriesInstanceUid, currentVersion: 100000, cancellationToken: tokenSource.Token);

        // Add metadata for all instances in the given list
        await _metadataStore.StoreInstanceMetadataAsync(first, firstVersion);
        await _metadataStore.StoreInstanceMetadataAsync(second, secondVersion);

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch, tokenSource.Token);

        var actual = response.ResponseMetadata.ToList();
        Assert.Equal(2, actual.Count);
        ValidateResponseMetadataDataset(first, actual[0]);
        ValidateResponseMetadataDataset(second, actual[1]);
    }

    // Note that tests must use unique watermarks to ensure their metadata files do not collide with each other
    private (DicomDataset First, int FirstVersion, DicomDataset Second, int SecondVersion) SetupDatasetList(
        string studyInstanceUid,
        string seriesInstanceUid = null,
        int partitionKey = DefaultPartition.Key,
        int currentVersion = 0,
        CancellationToken cancellationToken = default)
    {
        DicomDataset dicomDataset1;
        DicomDataset dicomDataset2;

        if (seriesInstanceUid == null)
        {
            dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate());
            dicomDataset2 = CreateValidMetadataDataset(studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate());

            _instanceStore
                .GetInstanceIdentifiersInStudyAsync(partitionKey, studyInstanceUid, cancellationToken)
                .Returns(
                    new List<VersionedInstanceIdentifier>
                    {
                            dicomDataset1.ToVersionedInstanceIdentifier(version: currentVersion + 1),
                            dicomDataset2.ToVersionedInstanceIdentifier(version: currentVersion + 2),
                    });
        }
        else
        {
            dicomDataset1 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate());
            dicomDataset2 = CreateValidMetadataDataset(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate());

            _instanceStore
                .GetInstanceIdentifiersInSeriesAsync(partitionKey, studyInstanceUid, seriesInstanceUid, cancellationToken)
                .Returns(
                    new List<VersionedInstanceIdentifier>
                    {
                            dicomDataset1.ToVersionedInstanceIdentifier(version: currentVersion + 1),
                            dicomDataset2.ToVersionedInstanceIdentifier(version: currentVersion + 2),
                    });
        }

        return (dicomDataset1, currentVersion + 1, dicomDataset2, currentVersion + 2);
    }

    private static DicomDataset CreateValidMetadataDataset(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        return new DicomDataset()
        {
            { DicomTag.StudyInstanceUID, studyInstanceUid },
            { DicomTag.SeriesInstanceUID, seriesInstanceUid },
            { DicomTag.SOPInstanceUID, sopInstanceUid },
        };
    }

    private static void ValidateResponseMetadataDataset(DicomDataset storedDataset, DicomDataset retrievedDataset)
    {
        // Compare result datasets by serializing.
        Assert.Equal(
            JsonSerializer.Serialize(storedDataset, AppSerializerOptions.Json),
            JsonSerializer.Serialize(retrievedDataset, AppSerializerOptions.Json));
    }
}
