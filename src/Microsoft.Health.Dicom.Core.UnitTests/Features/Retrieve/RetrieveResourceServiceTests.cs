// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.IO;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

public class RetrieveResourceServiceTests
{
    private readonly IMetadataStore _metadataStore;
    private readonly RetrieveResourceService _retrieveResourceService;
    private readonly IInstanceStore _instanceStore;
    private readonly IFileStore _fileStore;
    private readonly ITranscoder _retrieveTranscoder;
    private readonly IFrameHandler _dicomFrameHandler;
    private readonly IAcceptHeaderHandler _acceptHeaderHandler;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly ILogger<RetrieveResourceService> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    private readonly string _studyInstanceUid = TestUidGenerator.Generate();
    private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _sopInstanceUid = TestUidGenerator.Generate();
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
    private readonly IInstanceMetadataCache _instanceMetadataCache;
    private readonly IFramesRangeCache _framesRangeCache;
    private readonly RetrieveMeter _retrieveMeter;
    private static readonly FileProperties DefaultFileProperties = new FileProperties() { Path = "default/path/0.dcm", ETag = "123", ContentLength = 123 };

    public RetrieveResourceServiceTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _fileStore = Substitute.For<IFileStore>();
        _retrieveTranscoder = Substitute.For<ITranscoder>();
        _dicomFrameHandler = Substitute.For<IFrameHandler>();
        _acceptHeaderHandler = new AcceptHeaderHandler(NullLogger<AcceptHeaderHandler>.Instance);
        _logger = NullLogger<RetrieveResourceService>.Instance;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _dicomRequestContextAccessor.RequestContext.DataPartition = Partition.Default;
        var retrieveConfigurationSnapshot = Substitute.For<IOptionsSnapshot<RetrieveConfiguration>>();
        retrieveConfigurationSnapshot.Value.Returns(new RetrieveConfiguration());
        _instanceMetadataCache = Substitute.For<IInstanceMetadataCache>();
        _framesRangeCache = Substitute.For<IFramesRangeCache>();
        _retrieveMeter = new RetrieveMeter();

        _metadataStore = Substitute.For<IMetadataStore>();
        _retrieveResourceService = new RetrieveResourceService(
            _instanceStore,
            _fileStore,
            _retrieveTranscoder,
            _dicomFrameHandler,
            _acceptHeaderHandler,
            _dicomRequestContextAccessor,
            _metadataStore,
            _instanceMetadataCache,
            _framesRangeCache,
            retrieveConfigurationSnapshot,
            _retrieveMeter,
            _logger);
    }

    [Fact]
    public async Task GivenNoStoredInstances_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifiersInStudyAsync(Partition.Default, _studyInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenStoredInstancesWhereOneIsMissingFile_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> instances = SetupInstanceIdentifiersList(ResourceType.Study);
        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });

        // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
        // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
        for (int i = 0; i < instances.Count - 1; i++)
        {
            InstanceMetadata instance = instances[i];
            KeyValuePair<DicomFile, Stream> stream = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(instance.VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 0, disposeStreams: false);
            _fileStore.GetStreamingFileAsync(instance.VersionedInstanceIdentifier.Version, instance.VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(stream.Value);
            _retrieveTranscoder.TranscodeFileAsync(stream.Value, "*").Returns(stream.Value);
        }

        // For the last identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
        _fileStore.GetStreamingFileAsync(instances.Last().VersionedInstanceIdentifier.Version, instances.Last().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Throws(new InstanceNotFoundException());

        var response = await _retrieveResourceService.GetInstanceResourceAsync(
                                new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                                DefaultCancellationToken);
        await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.GetStreamsAsync());
    }

    [Fact]
    public async Task GivenStoredInstances_WhenRetrieveRequestForStudy_ThenInstancesInStudyAreRetrievedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFiles = await Task.WhenAll(versionedInstanceIdentifiers.Select(
            x => RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier), _recyclableMemoryStreamManager)));

        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });
        int count = 0;
        foreach (var file in streamsAndStoredFiles)
        {
            _fileStore.GetStreamingFileAsync(count, Partition.Default, null, DefaultCancellationToken).Returns(file.Value);
            count++;
        }

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
               DefaultCancellationToken);

        // Validate response status code and ensure response streams have expected files - they should be equivalent to what the store was set up to return.
        ValidateResponseStreams(streamsAndStoredFiles.Select(x => x.Key), await response.GetStreamsAsync());

        // Validate dicom request is populated with correct transcode values
        ValidateDicomRequestIsPopulated();

        // Dispose created streams.
        streamsAndStoredFiles.ToList().ForEach(x => x.Value.Dispose());

        // Validate instance count is added to dicom request context
        Assert.Equal(streamsAndStoredFiles.Length, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenStoredInstancesWithOriginalVersion_WhenRetrieveRequestForStudyForOriginal_ThenInstancesInStudyAreRetrievedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Study,
            instanceProperty: new InstanceProperties() { HasFrameMetadata = true, OriginalVersion = 5 },
            isOriginalVersion: true);

        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }, isOriginalVersionRequested: true),
               DefaultCancellationToken);

        await response.GetStreamsAsync();

        await _fileStore
            .Received(3)
            .GetStreamingFileAsync(Arg.Is<long>(x => x == 5), Partition.Default, null, DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenStoredInstancesWithOriginalVersion_WhenRetrieveRequestForStudyForLatest_ThenInstancesInStudyAreRetrievedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Study,
            instanceProperty: new InstanceProperties() { HasFrameMetadata = true, OriginalVersion = 5 });

        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
               DefaultCancellationToken);

        await response.GetStreamsAsync();

        await _fileStore
            .Received(1)
            .GetStreamingFileAsync(Arg.Is<long>(x => x == 0), Partition.Default, null, DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenInstancesWithOriginalVersion_WhenRetrieveRequestForStudyForOriginalWithTranscoding_ThenInstancesAreReturned()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        int originalVersion = 5;
        FileProperties fileProperties = new FileProperties { Path = "123.dcm", ETag = "e456", ContentLength = 123 };
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Instance,
            instanceProperty: new InstanceProperties()
            {
                HasFrameMetadata = true,
                OriginalVersion = originalVersion,
                TransferSyntaxUid = "1.2.840.10008.1.2.4.90",
                FileProperties = fileProperties
            },
            isOriginalVersion: true);
        _fileStore.GetFilePropertiesAsync(originalVersion, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, fileProperties, DefaultCancellationToken)
            .Returns(new FileProperties { ContentLength = new RetrieveConfiguration().MaxDicomFileSize });

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFiles = await Task.WhenAll(versionedInstanceIdentifiers.Select(
            x => RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier), _recyclableMemoryStreamManager)));

        _retrieveTranscoder.TranscodeFileAsync(Arg.Any<Stream>(), Arg.Any<string>()).Returns(streamsAndStoredFiles.First().Value);

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(
                   _studyInstanceUid,
                   _firstSeriesInstanceUid,
                   _sopInstanceUid,
                   new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy("1.2.840.10008.1.2.1") },
                   isOriginalVersionRequested: true),
               DefaultCancellationToken);

        await response.GetStreamsAsync();

        await _fileStore
            .Received(1)
            .GetFilePropertiesAsync(Arg.Is<long>(x => x == originalVersion), versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, versionedInstanceIdentifiers.First().InstanceProperties.FileProperties, DefaultCancellationToken);

        await _fileStore
            .DidNotReceive()
            .GetStreamingFileAsync(Arg.Any<long>(), versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken);

        await _fileStore
            .Received(1)
            .GetFileAsync(Arg.Is<long>(x => x == originalVersion), versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, fileProperties, DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenSpecificTransferSyntax_WhenRetrieveRequestForStudy_ThenInstancesInStudyAreTranscodedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        var instanceMetadata = new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, Partition.Default), new InstanceProperties());
        _instanceStore.GetInstanceIdentifierWithPropertiesAsync(_dicomRequestContextAccessor.RequestContext.DataPartition, _studyInstanceUid, null, null, false, DefaultCancellationToken).Returns(new[] { instanceMetadata });

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(instanceMetadata.VersionedInstanceIdentifier), _recyclableMemoryStreamManager);

        _fileStore.GetFileAsync(0, _dicomRequestContextAccessor.RequestContext.DataPartition, null, DefaultCancellationToken).Returns(streamsAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(instanceMetadata.VersionedInstanceIdentifier.Version, _dicomRequestContextAccessor.RequestContext.DataPartition, instanceMetadata.InstanceProperties.FileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamsAndStoredFile.Value.Length });
        string transferSyntax = "1.2.840.10008.1.2.1";
        _retrieveTranscoder.TranscodeFileAsync(streamsAndStoredFile.Value, transferSyntax).Returns(CopyStream(streamsAndStoredFile.Value));

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy(transferSyntax: transferSyntax) }),
               DefaultCancellationToken);

        // Validate response status code and ensure response streams have expected files - they should be equivalent to what the store was set up to return.
        ValidateResponseStreams(new[] { streamsAndStoredFile.Key }, await response.GetStreamsAsync());

        // Validate dicom request is populated with correct transcode values
        IEnumerable<Stream> streams = await response.GetStreamsAsync();
        ValidateDicomRequestIsPopulated(true, streams.Sum(s => s.Length));

        // Dispose created streams.
        streamsAndStoredFile.Value.Dispose();
    }

    [Fact]
    public async Task GivenNoStoredInstances_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifiersInSeriesAsync(Partition.Default, _studyInstanceUid, _firstSeriesInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenStoredInstancesWhereOneIsMissingFile_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);
        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });

        // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
        for (int i = 0; i < versionedInstanceIdentifiers.Count - 1; i++)
        {
            InstanceMetadata instance = versionedInstanceIdentifiers[i];
            KeyValuePair<DicomFile, Stream> stream = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(instance.VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 0, disposeStreams: false);
            _fileStore.GetStreamingFileAsync(instance.VersionedInstanceIdentifier.Version, instance.VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(stream.Value);
            _retrieveTranscoder.TranscodeFileAsync(stream.Value, "*").Returns(stream.Value);
        }

        // For the last identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
        _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Throws(new InstanceNotFoundException());

        var response = await _retrieveResourceService.GetInstanceResourceAsync(
                                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                                DefaultCancellationToken);
        await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.GetStreamsAsync());
    }

    [Fact]
    public async Task GivenStoredInstances_WhenRetrieveRequestForSeries_ThenInstancesInSeriesAreRetrievedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFiles = await Task.WhenAll(versionedInstanceIdentifiers
            .Select(x => RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier), _recyclableMemoryStreamManager)));

        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });
        int count = 0;
        foreach (var file in streamsAndStoredFiles)
        {
            _fileStore.GetStreamingFileAsync(count, Partition.Default, null, DefaultCancellationToken).Returns(file.Value);
            count++;
        }

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(
                   _studyInstanceUid,
                   _firstSeriesInstanceUid,
                   new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
               DefaultCancellationToken);

        // Validate response status code and ensure response streams have expected files - they should be equivalent to what the store was set up to return.
        ValidateResponseStreams(streamsAndStoredFiles.Select(x => x.Key), await response.GetStreamsAsync());

        // Validate dicom request is populated with correct transcode values
        ValidateDicomRequestIsPopulated();

        // Dispose created streams.
        streamsAndStoredFiles.ToList().ForEach(x => x.Value.Dispose());

        // Validate instance count is added to dicom request context
        Assert.Equal(streamsAndStoredFiles.Length, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenNoStoredInstances_WhenRetrieveRequestForInstance_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifierAsync(Partition.Default, _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForInstance_ThenNotFoundIsThrown()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);
        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });

        // For the first instance identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
        _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Throws(new InstanceNotFoundException());

        var response = await _retrieveResourceService.GetInstanceResourceAsync(
                    new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
                    DefaultCancellationToken);
        await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.GetStreamsAsync());
    }

    [Theory]
    [InlineData(PayloadTypes.SinglePart, true)]
    [InlineData(PayloadTypes.SinglePart, false)]
    [InlineData(PayloadTypes.MultipartRelated, true)]
    [InlineData(PayloadTypes.MultipartRelated, false)]
    public async Task GivenStoredInstances_WhenRetrieveRequestForInstance_ThenInstanceIsRetrievedSuccessfully(PayloadTypes payloadTypes, bool withFileProperties)
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance, withFileProperties: withFileProperties);

        // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager);

        var expectedFileProperties = withFileProperties
            ? versionedInstanceIdentifiers.First().InstanceProperties.FileProperties
            : null;

        _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, expectedFileProperties, DefaultCancellationToken).Returns(streamAndStoredFile.Value);

        _fileStore.GetFilePropertiesAsync(Arg.Any<long>(), Partition.Default, expectedFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = 1000 });

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(
                   _studyInstanceUid,
                   _firstSeriesInstanceUid,
                   _sopInstanceUid,
                   new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance(payloadTypes: payloadTypes) }),
               DefaultCancellationToken);

        // Validate response status code and ensure response stream has expected file - it should be equivalent to what the store was set up to return.
        ValidateResponseStreams(new List<DicomFile>() { streamAndStoredFile.Key }, await response.GetStreamsAsync());

        // Validate dicom request is populated with correct transcode values
        ValidateDicomRequestIsPopulated();

        // Validate content type
        Assert.Equal(KnownContentTypes.ApplicationDicom, response.ContentType);

        await _fileStore.Received(1)
            .GetStreamingFileAsync(
                versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version,
                versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition,
                expectedFileProperties,
                DefaultCancellationToken);

        // Dispose created streams.
        streamAndStoredFile.Value.Dispose();

        // Validate instance count is added to dicom request context
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenStoredInstancesWithoutFrames_WhenRetrieveRequestForFrame_ThenNotFoundIsThrown()
    {
        // Add multiple instances to validate that we evaluate the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
        var framesToRequest = new List<int> { 0 };

        // For the instance, set up the fileStore to return a stream containing the file associated with the identifier with 3 frames.
        Stream streamOfStoredFiles = (await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 0)).Value;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(streamOfStoredFiles);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamOfStoredFiles.Length });

        var retrieveResourceRequest = new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });
        _dicomFrameHandler.GetFramesResourceAsync(streamOfStoredFiles, retrieveResourceRequest.Frames, true, "*").Throws(new FrameNotFoundException());

        // Request for a specific frame on the instance.
        await Assert.ThrowsAsync<FrameNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
               retrieveResourceRequest,
               DefaultCancellationToken));

        streamOfStoredFiles.Dispose();
    }

    [Fact]
    public async Task GivenStoredInstancesWithFrames_WhenRetrieveRequestForNonExistingFrame_ThenNotFoundIsThrown()
    {
        // Add multiple instances to validate that we evaluate the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
        var framesToRequest = new List<int> { 1, 4 };

        // For the instance, set up the fileStore to return a stream containing the file associated with the identifier with 3 frames.
        Stream streamOfStoredFiles = (await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3)).Value;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(streamOfStoredFiles);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamOfStoredFiles.Length });

        var retrieveResourceRequest = new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });
        _dicomFrameHandler.GetFramesResourceAsync(streamOfStoredFiles, retrieveResourceRequest.Frames, true, "*").Throws(new FrameNotFoundException());

        // Request 2 frames - one which exists and one which doesn't.
        await Assert.ThrowsAsync<FrameNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
               retrieveResourceRequest,
               DefaultCancellationToken));

        // Dispose the stream.
        streamOfStoredFiles.Dispose();
    }

    [Fact]
    public async Task GivenStoredInstancesWithFrames_WhenRetrieveRequestForFrames_ThenFramesInInstanceAreRetrievedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
        var framesToRequest = new List<int> { 1, 2 };

        // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3);
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamAndStoredFile.Value.Length });

        // Setup frame handler to return the frames as streams from the file.
        Stream[] frames = framesToRequest.Select(f => GetFrameFromFile(streamAndStoredFile.Key.Dataset, f)).ToArray();
        var retrieveResourceRequest = new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });
        _dicomFrameHandler.GetFramesResourceAsync(streamAndStoredFile.Value, retrieveResourceRequest.Frames, true, "*").Returns(frames);
        _retrieveTranscoder.TranscodeFileAsync(streamAndStoredFile.Value, "*").Returns(streamAndStoredFile.Value);

        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               retrieveResourceRequest,
               DefaultCancellationToken);

        IEnumerable<Stream> streams = await response.GetStreamsAsync();
        // Validate response status code and ensure response streams has expected frames - it should be equivalent to what the store was set up to return.
        AssertPixelDataEqual(DicomPixelData.Create(streamAndStoredFile.Key.Dataset).GetFrame(framesToRequest[0]), streams.ToList()[0]);
        AssertPixelDataEqual(DicomPixelData.Create(streamAndStoredFile.Key.Dataset).GetFrame(framesToRequest[1]), streams.ToList()[1]);

        // Validate dicom request is populated with correct transcode values
        ValidateDicomRequestIsPopulated();

        streamAndStoredFile.Value.Dispose();

        // Validate part count is equal to the number of frames returned
        Assert.Equal(2, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Theory]
    [InlineData("*", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1")]
    [InlineData("1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1")]
    [InlineData("1.2.840.10008.1.2.4.90", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.4.90")]
    public async Task GetInstances_WithAcceptType_ThenResponseContentTypeIsCorrect(string requestedTransferSyntax, string originalTransferSyntax, string expectedTransferSyntax)
    {
        // arrange object with originalTransferSyntax
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance, instanceProperty: new InstanceProperties() { TransferSyntaxUid = originalTransferSyntax, FileProperties = null });

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFilesArray = await Task.WhenAll(versionedInstanceIdentifiers.Select(
            x => RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier, originalTransferSyntax), _recyclableMemoryStreamManager)));
        var streamsAndStoredFiles = new List<KeyValuePair<DicomFile, Stream>>(streamsAndStoredFilesArray);
        streamsAndStoredFiles.ForEach(x => _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(x.Value));
        streamsAndStoredFiles.ForEach(x => _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(x.Value));
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamsAndStoredFiles.First().Value.Length });
        streamsAndStoredFiles.ForEach(x => _retrieveTranscoder.TranscodeFileAsync(x.Value, requestedTransferSyntax).Returns(CopyStream(x.Value)));

        // act
        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy(requestedTransferSyntax) }),
               DefaultCancellationToken);

        // assert
        await using IAsyncEnumerator<RetrieveResourceInstance> enumerator = response.ResponseInstances.GetAsyncEnumerator(DefaultCancellationToken);
        while (await enumerator.MoveNextAsync())
        {
            Assert.Equal(expectedTransferSyntax, enumerator.Current.TransferSyntaxUid);
        }
    }

    [Theory]
    [InlineData("*", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1")]
    [InlineData("1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1")]
    [InlineData("1.2.840.10008.1.2.1", "1.2.840.10008.1.2.4.57", "1.2.840.10008.1.2.1")]
    public async Task GetFrames_WithAcceptType_ThenResponseContentTypeIsCorrect(string requestedTransferSyntax, string originalTransferSyntax, string expectedTransferSyntax)
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames, instanceProperty: new InstanceProperties() { TransferSyntaxUid = originalTransferSyntax });
        var framesToRequest = new List<int> { 1, 2 };

        // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, originalTransferSyntax), _recyclableMemoryStreamManager, frames: 3);
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamAndStoredFile.Value.Length });

        // Setup frame handler to return the frames as streams from the file.
        Stream[] frames = framesToRequest.Select(f => GetFrameFromFile(streamAndStoredFile.Key.Dataset, f)).ToArray();
        var retrieveResourceRequest = new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(requestedTransferSyntax) });
        _dicomFrameHandler.GetFramesResourceAsync(streamAndStoredFile.Value, retrieveResourceRequest.Frames, true, "*").Returns(frames);

        // act
        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               retrieveResourceRequest,
               DefaultCancellationToken);
        // assert
        await using IAsyncEnumerator<RetrieveResourceInstance> enumerator = response.ResponseInstances.GetAsyncEnumerator(DefaultCancellationToken);
        while (await enumerator.MoveNextAsync())
        {
            Assert.Equal(expectedTransferSyntax, enumerator.Current.TransferSyntaxUid);
        }
    }

    [Fact]
    public async Task GetFrames_WithSinglePartAcceptOnMultipleFrames_Throws()
    {
        // arrange
        string requestedTransferSyntax = "*";

        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
        var framesToRequest = new List<int> { 1, 2 };
        var retrieveResourceRequest = new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(requestedTransferSyntax, KnownContentTypes.ApplicationOctetStream, null, PayloadTypes.SinglePart) });

        // act and assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _retrieveResourceService.GetInstanceResourceAsync(
               retrieveResourceRequest,
               DefaultCancellationToken));
    }

    [Theory]
    [InlineData("*", "1.2.840.10008.1.2.1", "*")] // this is the bug in old files, that is fixed for new files
    [InlineData("1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.1")]
    [InlineData("1.2.840.10008.1.2.4.90", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.4.90")]
    public async Task GetInstances_WithAcceptTypeOnOldFile_ThenResponseContentTypeWithBackCompatWorks(string requestedTransferSyntax, string originalTransferSyntax, string expectedTransferSyntax)
    {
        // arrange object with originalTransferSyntax as null from DB to show backcompat
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFilesArray = await Task.WhenAll(versionedInstanceIdentifiers.Select(
            x => RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier, originalTransferSyntax), _recyclableMemoryStreamManager)));
        var streamsAndStoredFiles = new List<KeyValuePair<DicomFile, Stream>>(streamsAndStoredFilesArray);
        streamsAndStoredFiles.ForEach(x => _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(x.Value));
        streamsAndStoredFiles.ForEach(x => _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(x.Value));
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamsAndStoredFiles.First().Value.Length });
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamsAndStoredFiles.First().Value.Length });
        streamsAndStoredFiles.ForEach(x => _retrieveTranscoder.TranscodeFileAsync(x.Value, requestedTransferSyntax).Returns(CopyStream(x.Value)));

        // act
        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy(requestedTransferSyntax) }),
               DefaultCancellationToken);

        // assert
        await using IAsyncEnumerator<RetrieveResourceInstance> enumerator = response.ResponseInstances.GetAsyncEnumerator(DefaultCancellationToken);
        while (await enumerator.MoveNextAsync())
        {
            Assert.Equal(expectedTransferSyntax, enumerator.Current.TransferSyntaxUid);
        }
    }

    [Fact]
    public async Task GetStudy_WithMultipleInstanceAndTranscoding_ThrowsNotSupported()
    {
        // arrange object with originalTransferSyntax
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

        // act and assert
        await Assert.ThrowsAsync<NotAcceptableException>(() =>
        _retrieveResourceService.GetInstanceResourceAsync(
               new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy(transferSyntax: "1.2.840.10008.1.2.4.90") }),
               DefaultCancellationToken));
    }

    [Fact]
    public async Task GetFrames_WithLargeFileSize_ThrowsNotSupported()
    {
        // arrange
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
        var framesToRequest = new List<int> { 1, 2 };
        // arrange fileSize to be greater than max supported
        long aboveMaxFileSize = new RetrieveConfiguration().MaxDicomFileSize + 1;
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken).Returns(new FileProperties { ContentLength = aboveMaxFileSize });

        // act and assert
        await Assert.ThrowsAsync<NotAcceptableException>(() =>
        _retrieveResourceService.GetInstanceResourceAsync(
              new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
              DefaultCancellationToken));

    }

    [Fact]
    public async Task GetFrames_WithNoTranscode_HitsCache()
    {
        // arrange
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames, instanceProperty: new InstanceProperties() { HasFrameMetadata = true });
        var framesToRequest = new List<int> { 1 };
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken)
            .Returns(new FileProperties { ContentLength = new RetrieveConfiguration().MaxDicomFileSize });

        // act
        await _retrieveResourceService.GetInstanceResourceAsync(
              new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
              DefaultCancellationToken);

        // assert
        var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, Partition.Default);
        await _instanceMetadataCache.Received(1).GetAsync(Arg.Any<object>(), identifier, Arg.Any<Func<InstanceIdentifier, CancellationToken, Task<InstanceMetadata>>>(), Arg.Any<CancellationToken>());
        await _framesRangeCache.Received(1).GetAsync(Arg.Any<object>(), Arg.Any<long>(), Arg.Any<Func<long, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFrames_WithNoTranscode_ReturnsFramesFromCurrentVersion()
    {
        // arrange
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Frames,
            instanceProperty: new InstanceProperties() { HasFrameMetadata = true });
        var framesToRequest = new List<int> { 1 };
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken)
            .Returns(new FileProperties { ContentLength = new RetrieveConfiguration().MaxDicomFileSize });

        // act
        await _retrieveResourceService.GetInstanceResourceAsync(
              new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
              DefaultCancellationToken);

        // assert
        var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, Partition.Default);
        await _instanceMetadataCache.Received(1).GetAsync(Arg.Any<object>(), identifier, Arg.Any<Func<InstanceIdentifier, CancellationToken, Task<InstanceMetadata>>>(), Arg.Any<CancellationToken>());
        await _framesRangeCache.Received(1).GetAsync(
            Arg.Any<object>(),
            Arg.Is<long>(x => x == 3),
            Arg.Any<Func<long, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>>>(),
            Arg.Any<CancellationToken>());

        await _fileStore.GetFileFrameAsync(
            Arg.Is<long>(x => x == 3),
            Partition.Default,
            Arg.Any<FrameRange>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFrames_WithUpdatedInstanceAndWithNoTranscode_ReturnsFramesFromOriginalVersion()
    {
        int originalVersion = 1;
        // arrange
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Frames,
            instanceProperty: new InstanceProperties() { HasFrameMetadata = true, OriginalVersion = originalVersion });
        var framesToRequest = new List<int> { 1 };
        _fileStore.GetFilePropertiesAsync(originalVersion, versionedInstanceIdentifiers.First().VersionedInstanceIdentifier.Partition, null, DefaultCancellationToken)
            .Returns(new FileProperties { ContentLength = new RetrieveConfiguration().MaxDicomFileSize });

        // act
        await _retrieveResourceService.GetInstanceResourceAsync(
              new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
              DefaultCancellationToken);

        // assert
        var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, Partition.Default);
        await _instanceMetadataCache.Received(1).GetAsync(Arg.Any<object>(), identifier, Arg.Any<Func<InstanceIdentifier, CancellationToken, Task<InstanceMetadata>>>(), Arg.Any<CancellationToken>());
        await _framesRangeCache.Received(1).GetAsync(
            Arg.Any<object>(),
            Arg.Is<long>(x => x == originalVersion),
            Arg.Any<Func<long, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>>>(),
            Arg.Any<CancellationToken>());

        await _fileStore.GetFileFrameAsync(
            Arg.Is<long>(x => x == originalVersion),
            Partition.Default,
            Arg.Any<FrameRange>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFramesWithFileProperties_WithNoTranscode_ExpectGetFileFrameAsyncUsedFileProperties()
    {
        // arrange
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(
            ResourceType.Frames,
            instanceProperty: new InstanceProperties() { HasFrameMetadata = true, FileProperties = DefaultFileProperties });
        InstanceMetadata instance = versionedInstanceIdentifiers[0];

        var framesToRequest = new List<int> { 1 };

        _fileStore.GetFilePropertiesAsync(
                instance.VersionedInstanceIdentifier.Version,
                instance.VersionedInstanceIdentifier.Partition,
                instance.InstanceProperties.FileProperties,
                DefaultCancellationToken)
            .Returns(new FileProperties { ContentLength = new RetrieveConfiguration().MaxDicomFileSize });

        Dictionary<int, FrameRange> range = new Dictionary<int, FrameRange>();
        range.Add(0, new FrameRange(0, 1));

        _framesRangeCache.GetAsync(
            instance.VersionedInstanceIdentifier.Version,
            instance.VersionedInstanceIdentifier.Version,
            Arg.Any<Func<long, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>>>(),
            Arg.Any<CancellationToken>()).Returns(range);

        // act
        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
            DefaultCancellationToken);

        List<RetrieveResourceInstance> fastFrames = await response.ResponseInstances.ToListAsync();
        Assert.NotEmpty(fastFrames);

        // assert
        await _fileStore.GetFileFrameAsync(
            Arg.Is<long>(x => x == instance.VersionedInstanceIdentifier.Version),
            Partition.Default,
            Arg.Any<FrameRange>(),
            Arg.Is<FileProperties>(f => f.Path == DefaultFileProperties.Path && f.ETag == DefaultFileProperties.ETag && f.ContentLength == DefaultFileProperties.ContentLength),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFramesWithFileProperties_WithUpdatedInstanceAndWithNoTranscode_ExpectGetFileFrameAsyncUsesFileProperties()
    {
        // arrange
        List<InstanceMetadata> instances = SetupInstanceIdentifiersList(
            ResourceType.Frames,
            instanceProperty: new InstanceProperties() { HasFrameMetadata = true, OriginalVersion = 1, FileProperties = DefaultFileProperties });

        var framesToRequest = new List<int> { 1 };

        var instance = instances[0];
        _fileStore.GetFilePropertiesAsync(instance.InstanceProperties.OriginalVersion.Value, instance.VersionedInstanceIdentifier.Partition, instance.InstanceProperties.FileProperties, DefaultCancellationToken)
            .Returns(new FileProperties { ContentLength = new RetrieveConfiguration().MaxDicomFileSize });

        Dictionary<int, FrameRange> range = new Dictionary<int, FrameRange>();
        range.Add(0, new FrameRange(0, 1));

        _framesRangeCache.GetAsync(
            instance.InstanceProperties.OriginalVersion,
            instance.InstanceProperties.OriginalVersion.Value,
            Arg.Any<Func<long, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>>>(),
            Arg.Any<CancellationToken>()).Returns(range);

        // act
        RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
              new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
              DefaultCancellationToken);

        List<RetrieveResourceInstance> fastFrames = await response.ResponseInstances.ToListAsync();
        Assert.NotEmpty(fastFrames);

        // assert
        await _fileStore.GetFileFrameAsync(
            Arg.Is<long>(x => x == instance.InstanceProperties.OriginalVersion.Value),
            Partition.Default,
            Arg.Any<FrameRange>(),
            Arg.Is<FileProperties>(f => f.Path == DefaultFileProperties.Path && f.ETag == DefaultFileProperties.ETag && f.ContentLength == DefaultFileProperties.ContentLength),
            Arg.Any<CancellationToken>());
    }

    private List<InstanceMetadata> SetupInstanceIdentifiersList(ResourceType resourceType, Partition partition = null, InstanceProperties instanceProperty = null, bool withFileProperties = false, bool isOriginalVersion = false)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();

        instanceProperty ??= withFileProperties ? new InstanceProperties { FileProperties = DefaultFileProperties } : new InstanceProperties();
        partition ??= _dicomRequestContextAccessor.RequestContext.DataPartition;

        switch (resourceType)
        {
            case ResourceType.Study:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partition), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 1, partition), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _secondSeriesInstanceUid, TestUidGenerator.Generate(), 2, partition), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(partition, _studyInstanceUid, null, null, isOriginalVersion, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
            case ResourceType.Series:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partition), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 1, partition), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(partition, _studyInstanceUid, _firstSeriesInstanceUid, null, isOriginalVersion, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
            case ResourceType.Instance:
            case ResourceType.Frames:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, 3, partition), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 4, partition), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(Arg.Is<Partition>(x => x.Key == partition.Key), _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, isOriginalVersion, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList.SkipLast(1).ToList());
                var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, partition);
                _instanceMetadataCache.GetAsync(Arg.Any<object>(), identifier, Arg.Any<Func<InstanceIdentifier, CancellationToken, Task<InstanceMetadata>>>(), Arg.Any<CancellationToken>()).Returns(dicomInstanceIdentifiersList.First());
                break;
        }
        return dicomInstanceIdentifiersList;
    }

    private void ValidateDicomRequestIsPopulated(bool isTranscodeRequested = false, long sizeOfTranscode = 0)
    {
        Assert.Equal(isTranscodeRequested, _dicomRequestContextAccessor.RequestContext.IsTranscodeRequested);
        Assert.Equal(sizeOfTranscode, _dicomRequestContextAccessor.RequestContext.BytesTranscoded);
    }

    private void ValidateResponseStreams(
        IEnumerable<DicomFile> expectedFiles,
        IEnumerable<Stream> responseStreams)
    {
        var responseFiles = responseStreams.Select(x => DicomFile.Open(x)).ToList();

        Assert.Equal(expectedFiles.Count(), responseFiles.Count);

        foreach (DicomFile expectedFile in expectedFiles)
        {
            DicomFile actualFile = responseFiles.First(x => x.Dataset.ToInstanceIdentifier(Partition.Default).Equals(expectedFile
            .Dataset.ToInstanceIdentifier(Partition.Default)));

            // If the same transfer syntax as original, the files should be exactly the same
            if (expectedFile.Dataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
            {
                var expectedFileArray = FileToByteArray(expectedFile);
                var actualFileArray = FileToByteArray(actualFile);

                Assert.Equal(expectedFileArray.Length, actualFileArray.Length);

                for (var ii = 0; ii < expectedFileArray.Length; ii++)
                {
                    Assert.Equal(expectedFileArray[ii], actualFileArray[ii]);
                }
            }
            else
            {
                throw new NotImplementedException("Transcoded files do not have an implemented validation mechanism.");
            }
        }
    }

    private byte[] FileToByteArray(DicomFile file)
    {
        using MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream();
        file.Save(memoryStream);
        return memoryStream.ToArray();
    }

    private Stream GetFrameFromFile(DicomDataset dataset, int frame)
    {
        IByteBuffer frameData = DicomPixelData.Create(dataset).GetFrame(frame);
        return _recyclableMemoryStreamManager.GetStream("RetrieveResourceServiceTests.GetFrameFromFile", frameData.Data, 0, frameData.Data.Length);
    }

    private Stream CopyStream(Stream source)
    {
        MemoryStream dest = _recyclableMemoryStreamManager.GetStream();
        source.CopyTo(dest);
        dest.Seek(0, SeekOrigin.Begin);
        return dest;
    }

    private static void AssertPixelDataEqual(IByteBuffer expectedPixelData, Stream actualPixelData)
    {
        Assert.Equal(expectedPixelData.Size, actualPixelData.Length);
        Assert.Equal(0, actualPixelData.Position);
        for (var i = 0; i < expectedPixelData.Size; i++)
        {
            Assert.Equal(expectedPixelData.Data[i], actualPixelData.ReadByte());
        }
    }
}
