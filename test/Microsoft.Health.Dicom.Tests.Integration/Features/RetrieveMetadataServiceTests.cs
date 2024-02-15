// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Messages;
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
    private readonly Func<int> _getNextWatermark;
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IETagGenerator _eTagGenerator;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RetrieveMeter _retrieveMeter;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;

    private readonly string _studyInstanceUid = TestUidGenerator.Generate();
    private readonly string _seriesInstanceUid = TestUidGenerator.Generate();
    private readonly int _firstOriginalVersion;
    private readonly int _secondOriginalVersion;

    public RetrieveMetadataServiceTests(DataStoreTestsFixture storagefixture)
    {
        EnsureArg.IsNotNull(storagefixture, nameof(storagefixture));
        _getNextWatermark = () => storagefixture.NextWatermark;
        _instanceStore = Substitute.For<IInstanceStore>();
        _metadataStore = storagefixture.MetadataStore;
        _eTagGenerator = Substitute.For<IETagGenerator>();
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _retrieveMeter = new RetrieveMeter();
        _nameWithPrefix = storagefixture.NameWithPrefix;
        _dicomRequestContextAccessor.RequestContext.DataPartition = Partition.Default;

        _retrieveMetadataService = new RetrieveMetadataService(
            _instanceStore,
            _metadataStore,
            _eTagGenerator,
            _dicomRequestContextAccessor,
            _retrieveMeter,
            Options.Create(new RetrieveConfiguration()));

        _firstOriginalVersion = storagefixture.NextWatermark;
        _secondOriginalVersion = storagefixture.NextWatermark;
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();

        (_, VersionedDicomDataset second) = SetupDatasetList(ResourceType.Study, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for only one instance in the given list
        await _metadataStore.StoreInstanceMetadataAsync(second.Dataset, second.Version);

        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, cancellationToken: tokenSource.Token);
        await Assert.ThrowsAsync<ItemNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudy_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();

        SetupDatasetList(ResourceType.Study, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, cancellationToken: tokenSource.Token);
        await Assert.ThrowsAsync<ItemNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudy_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
    {
        using var tokenSource = new CancellationTokenSource();

        (VersionedDicomDataset first, VersionedDicomDataset second) = SetupDatasetList(ResourceType.Study, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for all instances in the given list
        await _metadataStore.StoreInstanceMetadataAsync(first.Dataset, first.Version);
        await _metadataStore.StoreInstanceMetadataAsync(second.Dataset, second.Version);

        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, cancellationToken: tokenSource.Token);
        await ValidateResponseMetadataAsync(response.ResponseMetadata, first, second);
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();

        (_, VersionedDicomDataset second) = SetupDatasetList(ResourceType.Series, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for only one instance in the given list
        await _metadataStore.StoreInstanceMetadataAsync(second.Dataset, second.Version);

        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, ifNoneMatch, cancellationToken: tokenSource.Token);
        await Assert.ThrowsAsync<ItemNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForSeries_WhenFailsToRetrieveAny_ThenNotFoundIsThrown()
    {
        using var tokenSource = new CancellationTokenSource();

        SetupDatasetList(ResourceType.Series, cancellationToken: tokenSource.Token);

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, ifNoneMatch, cancellationToken: tokenSource.Token);
        await Assert.ThrowsAsync<ItemNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForSeries_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
    {
        using var tokenSource = new CancellationTokenSource();

        (VersionedDicomDataset first, VersionedDicomDataset second) = SetupDatasetList(ResourceType.Series, cancellationToken: tokenSource.Token);

        // Add metadata for all instances in the given list
        await _metadataStore.StoreInstanceMetadataAsync(first.Dataset, first.Version);
        await _metadataStore.StoreInstanceMetadataAsync(second.Dataset, second.Version);

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, ifNoneMatch, cancellationToken: tokenSource.Token);
        await ValidateResponseMetadataAsync(response.ResponseMetadata, first, second);
    }

    [Fact]
    public async Task GivenFileIdentifier_WhenGetInstanceFramesRangeWithInvalidVersion_ShouldThrowExceptionAndAttemptTwice()
    {
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _metadataStore.GetInstanceFramesRangeAsync(1, CancellationToken.None));
        _nameWithPrefix.Received(1).GetInstanceFramesRangeFileNameWithSpace(1);
        _nameWithPrefix.Received(1).GetInstanceFramesRangeFileName(1);
    }

    [Fact]
    public async Task GivenRetrieveMetadataRequestForStudyWithOriginalVersion_WhenIsSuccessful_ThenInstanceMetadataIsRetrievedSuccessfully()
    {
        using var tokenSource = new CancellationTokenSource();

        (VersionedDicomDataset first, VersionedDicomDataset second) = SetupDatasetList(ResourceType.Study, isOriginalVersion: true, cancellationToken: tokenSource.Token);
        string ifNoneMatch = null;

        // Add metadata for all instances in the given list
        await _metadataStore.StoreInstanceMetadataAsync(first.Dataset, first.OriginalVersion);
        await _metadataStore.StoreInstanceMetadataAsync(second.Dataset, second.OriginalVersion);

        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, isOriginalVersionRequested: true, tokenSource.Token);
        await ValidateResponseMetadataAsync(response.ResponseMetadata, first, second);
    }

    // Note that tests must use unique watermarks to ensure their metadata files do not collide with each other
    private (VersionedDicomDataset First, VersionedDicomDataset Second) SetupDatasetList(
        ResourceType resourceType,
        bool isOriginalVersion = false,
        CancellationToken cancellationToken = default)
    {
        var instanceProperty1 = new InstanceProperties { OriginalVersion = _firstOriginalVersion };
        var instanceProperty2 = new InstanceProperties { OriginalVersion = _secondOriginalVersion };

        var seriesInstanceUid = resourceType == ResourceType.Study ? TestUidGenerator.Generate() : _seriesInstanceUid;
        var result1 = new VersionedDicomDataset
        {
            Dataset = CreateValidMetadataDataset(_studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate()),
            Version = _getNextWatermark(),
            OriginalVersion = _firstOriginalVersion,
        };

        var result2 = new VersionedDicomDataset
        {
            Dataset = CreateValidMetadataDataset(_studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate()),
            Version = _getNextWatermark(),
            OriginalVersion = _secondOriginalVersion,
        };

        if (resourceType == ResourceType.Study)
        {
            _instanceStore
                .GetInstanceIdentifierWithPropertiesAsync(_dicomRequestContextAccessor.RequestContext.DataPartition, _studyInstanceUid, isInitialVersion: isOriginalVersion, cancellationToken: cancellationToken)
                .Returns(
                    new List<InstanceMetadata>
                    {
                        new InstanceMetadata(result1.Dataset.ToVersionedInstanceIdentifier(version: result1.Version, Partition.Default), instanceProperty1),
                        new InstanceMetadata(result2.Dataset.ToVersionedInstanceIdentifier(version: result2.Version, Partition.Default), instanceProperty2),
                    });
        }
        else
        {
            _instanceStore
                .GetInstanceIdentifierWithPropertiesAsync(_dicomRequestContextAccessor.RequestContext.DataPartition, _studyInstanceUid, seriesInstanceUid, isInitialVersion: isOriginalVersion, cancellationToken: cancellationToken)
                .Returns(
                    new List<InstanceMetadata>
                    {
                        new InstanceMetadata(result1.Dataset.ToVersionedInstanceIdentifier(version: result1.Version, Partition.Default), instanceProperty1),
                        new InstanceMetadata(result2.Dataset.ToVersionedInstanceIdentifier(version: result2.Version, Partition.Default), instanceProperty2),
                    });
        }

        return (result1, result2);
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

    private static async Task ValidateResponseMetadataAsync(IAsyncEnumerable<DicomDataset> actual, params VersionedDicomDataset[] expected)
    {
        // Compare result datasets by serializing.
        var set = await actual.Select(x => JsonSerializer.Serialize(x, AppSerializerOptions.Json)).ToHashSetAsync();

        Assert.Equal(expected.Length, set.Count);
        foreach (string e in expected.Select(x => JsonSerializer.Serialize(x.Dataset, AppSerializerOptions.Json)))
        {
            Assert.True(set.Remove(e));
        }
    }

    private readonly struct VersionedDicomDataset
    {
        public DicomDataset Dataset { get; init; }

        public int Version { get; init; }

        public int OriginalVersion { get; init; }
    }
}
