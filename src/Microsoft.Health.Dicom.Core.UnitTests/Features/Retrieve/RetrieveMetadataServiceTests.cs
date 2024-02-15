// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

public class RetrieveMetadataServiceTests
{
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IETagGenerator _eTagGenerator;
    private readonly RetrieveMetadataService _retrieveMetadataService;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RetrieveMeter _retrieveMeter;

    private readonly string _studyInstanceUid = TestUidGenerator.Generate();
    private readonly string _seriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _sopInstanceUid = TestUidGenerator.Generate();
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

    public RetrieveMetadataServiceTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _eTagGenerator = Substitute.For<IETagGenerator>();
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _retrieveMeter = new RetrieveMeter();

        _dicomRequestContextAccessor.RequestContext.DataPartition = Partition.Default;
        _retrieveMetadataService = new RetrieveMetadataService(
            _instanceStore,
            _metadataStore,
            _eTagGenerator,
            _dicomRequestContextAccessor,
            _retrieveMeter,
            Options.Create(new RetrieveConfiguration()));
    }

    [Fact]
    public async Task GivenRetrieveStudyMetadataRequest_WhenStudyInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(TestUidGenerator.Generate(), ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified study cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyAndSeriesInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified series cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveSeriesMetadataRequest_WhenStudyInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        SetupInstanceIdentifiersList(ResourceType.Series, _dicomRequestContextAccessor.RequestContext.DataPartition);

        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(TestUidGenerator.Generate(), _seriesInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified series cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveSeriesMetadataRequest_WhenSeriesInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        SetupInstanceIdentifiersList(ResourceType.Series, _dicomRequestContextAccessor.RequestContext.DataPartition);

        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, TestUidGenerator.Generate(), ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified series cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudySeriesAndSopInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified instance cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveSopInstanceMetadataRequest_WhenStudyAndSeriesDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        SetupInstanceIdentifiersList(ResourceType.Instance, _dicomRequestContextAccessor.RequestContext.DataPartition);

        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), _sopInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified instance cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveSopInstanceMetadataRequest_WhenSeriesInstanceUidDoesNotExist_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        SetupInstanceIdentifiersList(ResourceType.Instance, _dicomRequestContextAccessor.RequestContext.DataPartition);

        string ifNoneMatch = null;
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, TestUidGenerator.Generate(), _sopInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken));
        Assert.Equal("The specified instance cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenFailsToRetrieveSome_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study, _dicomRequestContextAccessor.RequestContext.DataPartition);

        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier.Version, Arg.Any<CancellationToken>()).Throws(new InstanceNotFoundException());
        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, Arg.Any<CancellationToken>()).Returns(new DicomDataset());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken);
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
        Assert.Equal("The specified instance cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestForStudy_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study, _dicomRequestContextAccessor.RequestContext.DataPartition);

        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, DefaultCancellationToken).Returns(new DicomDataset());
        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier.Version, DefaultCancellationToken).Returns(new DicomDataset());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken);

        Assert.Equal(await response.ResponseMetadata.CountAsync(), versionedInstanceIdentifiers.Count);
        Assert.Equal(await response.ResponseMetadata.CountAsync(), _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestWithOriginalVersionForStudy_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
    {
        IReadOnlyList<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Study,
            instanceProperty: new InstanceProperties() { OriginalVersion = 5 },
            isInitialVersion: true);

        _metadataStore.GetInstanceMetadataAsync(5, DefaultCancellationToken).Returns(new DicomDataset());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(_studyInstanceUid, ifNoneMatch, isOriginalVersionRequested: true, DefaultCancellationToken);

        await response.ResponseMetadata.CountAsync();

        await _metadataStore
            .Received(2)
            .GetInstanceMetadataAsync(Arg.Is<long>(x => x == 5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenFailsToRetrieveSome_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series, _dicomRequestContextAccessor.RequestContext.DataPartition);

        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier.Version, Arg.Any<CancellationToken>()).Throws(new InstanceNotFoundException());
        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, Arg.Any<CancellationToken>()).Returns(new DicomDataset());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken);
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
        Assert.Equal("The specified instance cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestForSeries_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series, _dicomRequestContextAccessor.RequestContext.DataPartition);

        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, DefaultCancellationToken).Returns(new DicomDataset());
        _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier.Version, DefaultCancellationToken).Returns(new DicomDataset());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken);

        Assert.Equal(await response.ResponseMetadata.CountAsync(), versionedInstanceIdentifiers.Count);
        Assert.Equal(await response.ResponseMetadata.CountAsync(), _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenFailsToRetrieve_ThenDicomInstanceNotFoundExceptionIsThrownAsync()
    {
        InstanceMetadata sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance, _dicomRequestContextAccessor.RequestContext.DataPartition).First();

        _metadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier.VersionedInstanceIdentifier.Version, Arg.Any<CancellationToken>()).Throws(new InstanceNotFoundException());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken);
        InstanceNotFoundException exception = await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.ResponseMetadata.ToListAsync().AsTask());
        Assert.Equal("The specified instance cannot be found.", exception.Message);
    }

    [Fact]
    public async Task GivenRetrieveInstanceMetadataRequestForInstance_WhenIsSuccessful_ThenSuccessStatusCodeIsReturnedAsync()
    {
        InstanceMetadata sopInstanceIdentifier = SetupInstanceIdentifiersList(ResourceType.Instance, _dicomRequestContextAccessor.RequestContext.DataPartition).First();

        _metadataStore.GetInstanceMetadataAsync(sopInstanceIdentifier.VersionedInstanceIdentifier.Version, DefaultCancellationToken).Returns(new DicomDataset());

        string ifNoneMatch = null;
        RetrieveMetadataResponse response = await _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, ifNoneMatch, cancellationToken: DefaultCancellationToken);

        Assert.Equal(1, await response.ResponseMetadata.CountAsync());
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    private List<InstanceMetadata> SetupInstanceIdentifiersList(ResourceType resourceType, Partition partition = null, InstanceProperties instanceProperty = null, bool isInitialVersion = false)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();

        instanceProperty = instanceProperty ?? new InstanceProperties();
        partition = partition ?? Partition.Default;

        switch (resourceType)
        {
            case ResourceType.Study:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 0), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(Arg.Is<Partition>(x => x.Key == partition.Key), _studyInstanceUid, isInitialVersion: isInitialVersion, cancellationToken: DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
            case ResourceType.Series:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 0), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 1), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(Arg.Is<Partition>(x => x.Key == partition.Key), _studyInstanceUid, _seriesInstanceUid, isInitialVersion: isInitialVersion, cancellationToken: DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
            case ResourceType.Instance:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, version: 0), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(Arg.Is<Partition>(x => x.Key == partition.Key), _studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, isInitialVersion: isInitialVersion, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
        }

        return dicomInstanceIdentifiersList;
    }
}
