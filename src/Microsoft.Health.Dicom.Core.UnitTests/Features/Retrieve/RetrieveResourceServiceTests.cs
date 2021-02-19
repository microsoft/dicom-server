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
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class RetrieveResourceServiceTests
    {
        private readonly RetrieveResourceService _retrieveResourceService;
        private readonly IInstanceStore _instanceStore;
        private readonly IFileStore _fileStore;
        private readonly ITranscoder _retrieveTranscoder;
        private readonly IFrameHandler _dicomFrameHandler;
        private readonly IRetrieveTransferSyntaxHandler _retrieveTransferSyntaxHandler;
        private readonly ILogger<RetrieveResourceService> _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _sopInstanceUid = TestUidGenerator.Generate();
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        public RetrieveResourceServiceTests()
        {
            _instanceStore = Substitute.For<IInstanceStore>();
            _fileStore = Substitute.For<IFileStore>();
            _retrieveTranscoder = Substitute.For<ITranscoder>();
            _dicomFrameHandler = Substitute.For<IFrameHandler>();
            _retrieveTransferSyntaxHandler = new RetrieveTransferSyntaxHandler(NullLogger<RetrieveTransferSyntaxHandler>.Instance);
            _logger = NullLogger<RetrieveResourceService>.Instance;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _retrieveResourceService = new RetrieveResourceService(
                _instanceStore, _fileStore, _retrieveTranscoder, _dicomFrameHandler, _retrieveTransferSyntaxHandler, _logger);
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
        {
            _instanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWhereOneIsMissingFile_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
            versionedInstanceIdentifiers.SkipLast(1).Select(x => _fileStore.GetFileAsync(x, DefaultCancellationToken).Returns(
                StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x), frames: 0, disposeStreams: true).Result.Value));

            // For the last identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
            _fileStore.GetFileAsync(versionedInstanceIdentifiers.Last(), DefaultCancellationToken).Throws(new InstanceNotFoundException());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForStudy_ThenInstancesInStudyAreRetrievedSuccesfully()
        {
            // Add multiple instances to validate that we return the requested instance and ignore the other(s).
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
            var streamsAndStoredFiles = versionedInstanceIdentifiers.Select(
                x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x)).Result).ToList();

            streamsAndStoredFiles.ForEach(x => _fileStore.GetFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));

            RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
                   new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                   DefaultCancellationToken);

            // Validate response status code and ensure response streams have expected files - they should be equivalent to what the store was set up to return.
            ValidateResponseStreams(streamsAndStoredFiles.Select(x => x.Key), response.ResponseStreams);

            // Dispose created streams.
            streamsAndStoredFiles.ToList().ForEach(x => x.Value.Dispose());
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
        {
            _instanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _firstSeriesInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWhereOneIsMissingFile_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            // For each instance identifier but the last, set up the fileStore to return a stream containing a file associated with the identifier.
            versionedInstanceIdentifiers.SkipLast(1).Select(x => _fileStore.GetFileAsync(x, DefaultCancellationToken).Returns(
                StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x), frames: 0, disposeStreams: true).Result.Value));

            // For the last identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
            _fileStore.GetFileAsync(versionedInstanceIdentifiers.Last(), DefaultCancellationToken).Throws(new InstanceNotFoundException());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForSeries_ThenInstancesInSeriesAreRetrievedSuccesfully()
        {
            // Add multiple instances to validate that we return the requested instance and ignore the other(s).
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            // For each instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
            var streamsAndStoredFiles = versionedInstanceIdentifiers
                .Select(x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x)).Result)
                .ToList();

            streamsAndStoredFiles.ForEach(x => _fileStore.GetFileAsync(x.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(x.Value));

            RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
                   new RetrieveResourceRequest(
                       _studyInstanceUid,
                       _firstSeriesInstanceUid,
                       new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                   DefaultCancellationToken);

            // Validate response status code and ensure response streams have expected files - they should be equivalent to what the store was set up to return.
            ValidateResponseStreams(streamsAndStoredFiles.Select(x => x.Key), response.ResponseStreams);

            // Dispose created streams.
            streamsAndStoredFiles.ToList().ForEach(x => x.Value.Dispose());
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForInstance_ThenNotFoundIsThrown()
        {
            _instanceStore.GetInstanceIdentifierAsync(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForInstance_ThenNotFoundIsThrown()
        {
            // Add multiple instances to validate that we return the requested instance and ignore the other(s).
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

            // For the first instance identifier, set up the fileStore to throw a store exception with the status code 404 (NotFound).
            _fileStore.GetFileAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Throws(new InstanceNotFoundException());

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForInstance_ThenInstanceIsRetrievedSuccessfully()
        {
            // Add multiple instances to validate that we return the requested instance and ignore the other(s).
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

            // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
            KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First())).Result;
            _fileStore.GetFileAsync(streamAndStoredFile.Key.Dataset.ToVersionedInstanceIdentifier(0), DefaultCancellationToken).Returns(streamAndStoredFile.Value);

            RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
                   new RetrieveResourceRequest(
                       _studyInstanceUid,
                       _firstSeriesInstanceUid,
                       _sopInstanceUid,
                       new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetInstance() }),
                   DefaultCancellationToken);

            // Validate response status code and ensure response stream has expected file - it should be equivalent to what the store was set up to return.
            ValidateResponseStreams(new List<DicomFile>() { streamAndStoredFile.Key }, response.ResponseStreams);

            // Dispose created streams.
            streamAndStoredFile.Value.Dispose();
        }

        [Fact]
        public async Task GivenStoredInstancesWithoutFrames_WhenRetrieveRequestForFrame_ThenNotFoundIsThrown()
        {
            // Add multiple instances to validate that we evaluate the requested instance and ignore the other(s).
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
            var framesToRequest = new List<int> { 0 };

            // For the instance, set up the fileStore to return a stream containing the file associated with the identifier with 3 frames.
            Stream streamOfStoredFiles = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First()), frames: 0).Result.Value;
            _fileStore.GetFileAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(streamOfStoredFiles);

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
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
            var framesToRequest = new List<int> { 1, 4 };

            // For the instance, set up the fileStore to return a stream containing the file associated with the identifier with 3 frames.
            Stream streamOfStoredFiles = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First()), frames: 3).Result.Value;
            _fileStore.GetFileAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(streamOfStoredFiles);

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
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);
            var framesToRequest = new List<int> { 1, 2 };

            // For the first instance identifier, set up the fileStore to return a stream containing a file associated with the identifier.
            KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First()), frames: 3).Result;
            _fileStore.GetFileAsync(versionedInstanceIdentifiers.First(), DefaultCancellationToken).Returns(streamAndStoredFile.Value);

            // Setup frame handler to return the frames as streams from the file.
            Stream[] frames = framesToRequest.Select(f => GetFrameFromFile(streamAndStoredFile.Key.Dataset, f)).ToArray();
            var retrieveResourceRequest = new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, framesToRequest, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame() });
            _dicomFrameHandler.GetFramesResourceAsync(streamAndStoredFile.Value, retrieveResourceRequest.Frames, true, "*").Returns(frames);

            RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
                   retrieveResourceRequest,
                   DefaultCancellationToken);

            // Validate response status code and ensure response streams has expected frames - it should be equivalent to what the store was set up to return.
            AssertPixelDataEqual(DicomPixelData.Create(streamAndStoredFile.Key.Dataset).GetFrame(framesToRequest[0]), response.ResponseStreams.ToList()[0]);
            AssertPixelDataEqual(DicomPixelData.Create(streamAndStoredFile.Key.Dataset).GetFrame(framesToRequest[1]), response.ResponseStreams.ToList()[1]);

            streamAndStoredFile.Value.Dispose();
        }

        private List<VersionedInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            var dicomInstanceIdentifiersList = new List<VersionedInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _secondSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    _instanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    _instanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _firstSeriesInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Instance:
                case ResourceType.Frames:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    _instanceStore.GetInstanceIdentifierAsync(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList.SkipLast(1));
                    break;
            }

            return dicomInstanceIdentifiersList;
        }

        private DicomDataset GenerateDatasetsFromIdentifiers(InstanceIdentifier instanceIdentifier)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
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
}
