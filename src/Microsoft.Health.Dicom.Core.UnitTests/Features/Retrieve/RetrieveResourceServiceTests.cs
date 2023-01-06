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
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
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
        _dicomRequestContextAccessor.RequestContext.DataPartitionEntry = PartitionEntry.Default;
        var retrieveConfigurationSnapshot = Substitute.For<IOptionsSnapshot<RetrieveConfiguration>>();
        retrieveConfigurationSnapshot.Value.Returns(new RetrieveConfiguration());
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _instanceMetadataCache = Substitute.For<IInstanceMetadataCache>();
        _framesRangeCache = Substitute.For<IFramesRangeCache>();

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
            _logger,
            loggerFactory
            );
    }

    [Fact]
    public async Task GivenNoStoredInstances_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifiersInStudyAsync(DefaultPartition.Key, _studyInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenStoredInstancesWhereOneIsMissingFile_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> instances = SetupInstanceIdentifiersList(ResourceType.Study);
        _fileStore.GetFilePropertiesAsync(Arg.Any<VersionedInstanceIdentifier>(), DefaultCancellationToken).Returns(new FileProperties() { ContentLength = 1000 });

        // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
        // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
        for (int i = 0; i < instances.Count - 1; i++)
        {
            InstanceMetadata instance = instances[i];
            KeyValuePair<DicomFile, Stream> stream = await StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instance.VersionedInstanceIdentifier), frames: 0, disposeStreams: false);
            _fileStore.GetStreamingFileAsync(instance.VersionedInstanceIdentifier, DefaultCancellationToken).Returns(stream.Value);
            _retrieveTranscoder.TranscodeFileAsync(stream.Value, "*").Returns(stream.Value);
        }

        // For the last identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
        _fileStore.GetStreamingFileAsync(instances.Last().VersionedInstanceIdentifier, DefaultCancellationToken).Throws(new InstanceNotFoundException());

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
        var streamsAndStoredFiles = versionedInstanceIdentifiers.Select(
            x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier)).Result).ToList();

        _fileStore.GetFilePropertiesAsync(Arg.Any<VersionedInstanceIdentifier>(), DefaultCancellationToken).Returns(new FileProperties() { ContentLength = 1000 });
        streamsAndStoredFiles.ForEach(x => _fileStore.GetStreamingFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));

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
        Assert.Equal(streamsAndStoredFiles.Count, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenSpecificTransferSyntax_WhenRetrieveRequestForStudy_ThenInstancesInStudyAreTranscodedSuccesfully()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        var instanceMetadata = new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, DefaultPartition.Key), new InstanceProperties());
        _instanceStore.GetInstanceIdentifierWithPropertiesAsync(DefaultPartition.Key, _studyInstanceUid, null, null, DefaultCancellationToken).Returns(new[] { instanceMetadata });

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFile = await StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instanceMetadata.VersionedInstanceIdentifier));

        _fileStore.GetFileAsync(streamsAndStoredFile.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(streamsAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(instanceMetadata.VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamsAndStoredFile.Value.Length });
        string transferSyntax = "1.2.840.10008.1.2.1";
        _retrieveTranscoder.TranscodeFileAsync(streamsAndStoredFile.Value, transferSyntax).Returns(streamsAndStoredFile.Value);

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
        _instanceStore.GetInstanceIdentifiersInSeriesAsync(DefaultPartition.Key, _studyInstanceUid, _firstSeriesInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenStoredInstancesWhereOneIsMissingFile_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);
        _fileStore.GetFilePropertiesAsync(Arg.Any<VersionedInstanceIdentifier>(), DefaultCancellationToken).Returns(new FileProperties() { ContentLength = 1000 });

        // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
        for (int i = 0; i < versionedInstanceIdentifiers.Count - 1; i++)
        {
            InstanceMetadata instance = versionedInstanceIdentifiers[i];
            KeyValuePair<DicomFile, Stream> stream = await StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instance.VersionedInstanceIdentifier), frames: 0, disposeStreams: false);
            _fileStore.GetStreamingFileAsync(instance.VersionedInstanceIdentifier, DefaultCancellationToken).Returns(stream.Value);
            _retrieveTranscoder.TranscodeFileAsync(stream.Value, "*").Returns(stream.Value);
        }

        // For the last identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
        _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.Last().VersionedInstanceIdentifier, DefaultCancellationToken).Throws(new InstanceNotFoundException());

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
        var streamsAndStoredFiles = versionedInstanceIdentifiers
            .Select(x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier)).Result)
            .ToList();

        _fileStore.GetFilePropertiesAsync(Arg.Any<VersionedInstanceIdentifier>(), DefaultCancellationToken).Returns(new FileProperties() { ContentLength = 1000 });
        streamsAndStoredFiles.ForEach(x => _fileStore.GetStreamingFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));

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
        Assert.Equal(streamsAndStoredFiles.Count, _dicomRequestContextAccessor.RequestContext.PartCount);
    }

    [Fact]
    public async Task GivenNoStoredInstances_WhenRetrieveRequestForInstance_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifierAsync(DefaultPartition.Key, _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
            new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForInstance_ThenNotFoundIsThrown()
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

        // For the first instance identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
        _fileStore.GetStreamingFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Throws(new InstanceNotFoundException());

        var response = await _retrieveResourceService.GetInstanceResourceAsync(
                    new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
                    DefaultCancellationToken);
        await Assert.ThrowsAsync<InstanceNotFoundException>(() => response.GetStreamsAsync());
    }

    [Theory]
    [InlineData(PayloadTypes.SinglePart)]
    [InlineData(PayloadTypes.MultipartRelated)]
    public async Task GivenStoredInstances_WhenRetrieveRequestForInstance_ThenInstanceIsRetrievedSuccessfully(PayloadTypes payloadTypes)
    {
        // Add multiple instances to validate that we return the requested instance and ignore the other(s).
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

        _fileStore.GetFilePropertiesAsync(Arg.Any<VersionedInstanceIdentifier>(), DefaultCancellationToken).Returns(new FileProperties() { ContentLength = 1000 });
        // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier)).Result;
        _fileStore.GetStreamingFileAsync(streamAndStoredFile.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(streamAndStoredFile.Value);

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
        Stream streamOfStoredFiles = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), frames: 0).Result.Value;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamOfStoredFiles);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamOfStoredFiles.Length });

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
        Stream streamOfStoredFiles = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), frames: 3).Result.Value;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamOfStoredFiles);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamOfStoredFiles.Length });

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
        KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), frames: 3).Result;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamAndStoredFile.Value.Length });

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
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance, DefaultPartition.Key, new InstanceProperties() { TransferSyntaxUid = originalTransferSyntax });

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFiles = versionedInstanceIdentifiers.Select(
            x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier, originalTransferSyntax)).Result).ToList();
        streamsAndStoredFiles.ForEach(x => _fileStore.GetFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));
        streamsAndStoredFiles.ForEach(x => _fileStore.GetStreamingFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamsAndStoredFiles.First().Value.Length });
        streamsAndStoredFiles.ForEach(x => _retrieveTranscoder.TranscodeFileAsync(x.Value, requestedTransferSyntax).Returns(x.Value));

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
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames, DefaultPartition.Key, new InstanceProperties() { TransferSyntaxUid = originalTransferSyntax });
        var framesToRequest = new List<int> { 1, 2 };

        // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, originalTransferSyntax), frames: 3).Result;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamAndStoredFile.Value.Length });

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
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance, DefaultPartition.Key, null);

        // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
        var streamsAndStoredFiles = versionedInstanceIdentifiers.Select(
            x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x.VersionedInstanceIdentifier, originalTransferSyntax)).Result).ToList();
        streamsAndStoredFiles.ForEach(x => _fileStore.GetFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));
        streamsAndStoredFiles.ForEach(x => _fileStore.GetStreamingFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamsAndStoredFiles.First().Value.Length });
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamsAndStoredFiles.First().Value.Length });
        streamsAndStoredFiles.ForEach(x => _retrieveTranscoder.TranscodeFileAsync(x.Value, requestedTransferSyntax).Returns(x.Value));

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
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = aboveMaxFileSize });

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
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames, DefaultPartition.Key, new InstanceProperties() { HasFrameMetadata = true });
        var framesToRequest = new List<int> { 1 };

        // act
        await _retrieveResourceService.GetInstanceResourceAsync(
              new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() }),
              DefaultCancellationToken);

        // assert
        var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, DefaultPartition.Key);
        await _instanceMetadataCache.Received(1).GetAsync(Arg.Any<object>(), identifier, Arg.Any<Func<InstanceIdentifier, CancellationToken, Task<InstanceMetadata>>>(), Arg.Any<CancellationToken>());
        await _framesRangeCache.Received(1).GetAsync(Arg.Any<object>(), Arg.Any<VersionedInstanceIdentifier>(), Arg.Any<Func<VersionedInstanceIdentifier, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>>>(), Arg.Any<CancellationToken>());
    }

    private List<InstanceMetadata> SetupInstanceIdentifiersList(ResourceType resourceType, int partitionKey = DefaultPartition.Key, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();

        instanceProperty = instanceProperty ?? new InstanceProperties();

        switch (resourceType)
        {
            case ResourceType.Study:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _secondSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(partitionKey, _studyInstanceUid, null, null, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
            case ResourceType.Series:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(partitionKey, _studyInstanceUid, _firstSeriesInstanceUid, null, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                break;
            case ResourceType.Instance:
            case ResourceType.Frames:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, 0, partitionKey), instanceProperty));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
                _instanceStore.GetInstanceIdentifierWithPropertiesAsync(partitionKey, _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList.SkipLast(1));
                var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, partitionKey);
                _instanceMetadataCache.GetAsync(Arg.Any<object>(), identifier, Arg.Any<Func<InstanceIdentifier, CancellationToken, Task<InstanceMetadata>>>(), Arg.Any<CancellationToken>()).Returns(dicomInstanceIdentifiersList.First());
                break;
        }
        return dicomInstanceIdentifiersList;
    }

    private static DicomDataset GenerateDatasetsFromIdentifiers(InstanceIdentifier instanceIdentifier, string transferSyntaxUid = null)
    {
        DicomTransferSyntax syntax = DicomTransferSyntax.ExplicitVRLittleEndian;
        if (transferSyntaxUid != null)
        {
            syntax = DicomTransferSyntax.Parse(transferSyntaxUid);
        }

        var ds = new DicomDataset(syntax)
        {
            { DicomTag.StudyInstanceUID, instanceIdentifier.StudyInstanceUid },
            { DicomTag.SeriesInstanceUID, instanceIdentifier.SeriesInstanceUid },
            { DicomTag.SOPInstanceUID, instanceIdentifier.SopInstanceUid },
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.PatientID, TestUidGenerator.Generate() },
            { DicomTag.BitsAllocated, (ushort)8 },
            { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
        };

        return ds;
    }

    private async Task<KeyValuePair<DicomFile, Stream>> StreamAndStoredFileFromDataset(DicomDataset dataset, int frames = 0, bool disposeStreams = false)
    {
        // Create DicomFile associated with input dataset with random pixel data.
        var dicomFile = new DicomFile(dataset);
        Samples.AppendRandomPixelData(5, 5, frames, dicomFile);

        if (disposeStreams)
        {
            using MemoryStream disposableStream = _recyclableMemoryStreamManager.GetStream();

            // Save file to a stream and reset position to 0.
            await dicomFile.SaveAsync(disposableStream);
            disposableStream.Position = 0;

            return new KeyValuePair<DicomFile, Stream>(dicomFile, disposableStream);
        }

        MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
        await dicomFile.SaveAsync(stream);
        stream.Position = 0;

        return new KeyValuePair<DicomFile, Stream>(dicomFile, stream);
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
            DicomFile actualFile = responseFiles.First(x => x.Dataset.ToInstanceIdentifier().Equals(expectedFile.Dataset.ToInstanceIdentifier()));

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
