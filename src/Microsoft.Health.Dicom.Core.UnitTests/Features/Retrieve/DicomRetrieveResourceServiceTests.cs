// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using DicomInstanceNotFoundException = Microsoft.Health.Dicom.Core.Exceptions.DicomInstanceNotFoundException;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class DicomRetrieveResourceServiceTests
    {
        private readonly DicomRetrieveResourceService _dicomRetrieveResourceService;
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomFileStore _dicomFileStore;
        private readonly ILogger<DicomRetrieveResourceService> _logger;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _sopInstanceUid = TestUidGenerator.Generate();
        private static readonly CancellationToken _defaultCancellationToken = new CancellationTokenSource().Token;

        public DicomRetrieveResourceServiceTests()
        {
            _dicomInstanceStore = Substitute.For<IDicomInstanceStore>();
            _dicomFileStore = Substitute.For<IDicomFileStore>();
            _logger = NullLogger<DicomRetrieveResourceService>.Instance;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _dicomRetrieveResourceService = new DicomRetrieveResourceService(_dicomInstanceStore, _dicomFileStore, _recyclableMemoryStreamManager, _logger);
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid).Returns(new List<VersionedDicomInstanceIdentifier>());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest("*", _studyInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenFailsToRetrieveOne_ThenNotFoundIsThrown()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            instanceIdentifiers.SkipLast(1).Select(x => _dicomFileStore.GetFileAsync(x, _defaultCancellationToken).Returns(
                StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x), frames: 0, disposeStreams: true).Result.Value));

            _dicomFileStore.GetFileAsync(instanceIdentifiers.Last(), _defaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest("*", _studyInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenIsSuccessful_ThenInstancesInStudyAreRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            List<KeyValuePair<DicomFile, Stream>> streamsAndStoredFiles = instanceIdentifiers.Select(
                x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x)).Result).ToList();

            foreach (var streamAndStoredFile in streamsAndStoredFiles)
            {
                _dicomFileStore.GetFileAsync(streamAndStoredFile.Key.Dataset.ToVersionedDicomInstanceIdentifier(0), _defaultCancellationToken).Returns(streamAndStoredFile.Value);
            }

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid),
                   _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            ValidateResponseStreams(streamsAndStoredFiles.Select(x => x.Key), response.ResponseStreams);

            streamsAndStoredFiles.ToList().ForEach(x => x.Value.Dispose());
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _firstSeriesInstanceUid).Returns(new List<VersionedDicomInstanceIdentifier>());

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenFailsToRetrieveOne_ThenNotFoundIsThrown()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            instanceIdentifiers.SkipLast(1).Select(x => _dicomFileStore.GetFileAsync(x, _defaultCancellationToken).Returns(
                StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x), frames: 0, disposeStreams: true).Result.Value));

            _dicomFileStore.GetFileAsync(instanceIdentifiers.Last(), _defaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenIsSuccessful_ThenInstancesInSeriesAreRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            List<KeyValuePair<DicomFile, Stream>> streamsAndStoredFiles = instanceIdentifiers.Select(
                x => StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(x)).Result).ToList();

            foreach (var streamAndStoredFile in streamsAndStoredFiles)
            {
                _dicomFileStore.GetFileAsync(streamAndStoredFile.Key.Dataset.ToVersionedDicomInstanceIdentifier(0), _defaultCancellationToken).Returns(streamAndStoredFile.Value);
            }

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid),
                   _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            ValidateResponseStreams(streamsAndStoredFiles.Select(x => x.Key), response.ResponseStreams);

            streamsAndStoredFiles.ToList().ForEach(x => x.Value.Dispose());
        }

        [Fact]
        public async Task GivenRetrieveRequestForInstance_WhenIsSuccessful_ThenInstanceIsRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

            KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First())).Result;
            _dicomFileStore.GetFileAsync(streamAndStoredFile.Key.Dataset.ToVersionedDicomInstanceIdentifier(0), _defaultCancellationToken).Returns(streamAndStoredFile.Value);

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid),
                   _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            ValidateResponseStreams(new List<DicomFile>() { streamAndStoredFile.Key }, response.ResponseStreams);

            streamAndStoredFile.Value.Dispose();
        }

        [Fact]
        public async Task GivenRetrieveRequestForFramesInInstance_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);

            Stream streamOfStoredFiles = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First()), frames: 0).Result.Value;
            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(streamOfStoredFiles);

            await Assert.ThrowsAsync<DicomFrameNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new List<int> { 1, 2 }),
                   _defaultCancellationToken));

            streamOfStoredFiles.Dispose();
        }

        [Fact]
        public async Task GivenRetrieveRequestForFramesInInstance_WhenFailsToRetrieveOne_ThenNotFoundIsThrown()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);

            Stream streamOfStoredFiles = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First()), frames: 3).Result.Value;
            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(streamOfStoredFiles);

            await Assert.ThrowsAsync<DicomFrameNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new List<int> { 1, 4 }),
                   _defaultCancellationToken));

            streamOfStoredFiles.Dispose();
        }

        [Fact]
        public async Task GivenRetrieveRequestForFramesInInstance_WhenIsSuccessful_ThenFramesInInstanceAreRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);

            KeyValuePair<DicomFile, Stream> streamAndStoredFile = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First()), frames: 3).Result;
            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(streamAndStoredFile.Value);

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new List<int> { 1, 2 }),
                   _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            AssertPixelDataEqual(DicomPixelData.Create(streamAndStoredFile.Key.Dataset).GetFrame(0), response.ResponseStreams.ToList()[0]);
            AssertPixelDataEqual(DicomPixelData.Create(streamAndStoredFile.Key.Dataset).GetFrame(1), response.ResponseStreams.ToList()[1]);

            streamAndStoredFile.Value.Dispose();
        }

        private List<VersionedDicomInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            var dicomInstanceIdentifiersList = new List<VersionedDicomInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _secondSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    _dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(_studyInstanceUid, _defaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    _dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(_studyInstanceUid, _firstSeriesInstanceUid, _defaultCancellationToken).Returns(dicomInstanceIdentifiersList);
                    break;
                case ResourceType.Instance:
                case ResourceType.Frames:
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, 0));
                    dicomInstanceIdentifiersList.Add(new VersionedDicomInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0));
                    _dicomInstanceStore.GetInstanceIdentifierAsync(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, _defaultCancellationToken).Returns(dicomInstanceIdentifiersList.SkipLast(1));
                    break;
            }

            return dicomInstanceIdentifiersList;
        }

        private DicomDataset GenerateDatasetsFromIdentifiers(DicomInstanceIdentifier dicomInstanceIdentifier)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.StudyInstanceUID, dicomInstanceIdentifier.StudyInstanceUid },
                { DicomTag.SeriesInstanceUID, dicomInstanceIdentifier.SeriesInstanceUid },
                { DicomTag.SOPInstanceUID, dicomInstanceIdentifier.SopInstanceUid },
                { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
                { DicomTag.BitsAllocated, (ushort)8 },
                { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
            };

            return ds;
        }

        private async Task<KeyValuePair<DicomFile, Stream>> StreamAndStoredFileFromDataset(DicomDataset dataset, int frames = 0, bool disposeStreams = false)
        {
            DicomFile dicomFile = new DicomFile(dataset);
            Samples.AppendRandomPixelData(5, 5, frames, dicomFile);

            if (disposeStreams)
            {
                using (MemoryStream disposableStream = _recyclableMemoryStreamManager.GetStream())
                {
                    await dicomFile.SaveAsync(disposableStream);
                    disposableStream.Position = 0;

                    return new KeyValuePair<DicomFile, Stream>(dicomFile, disposableStream);
                }
            }

            MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
            await dicomFile.SaveAsync(stream);
            stream.Position = 0;

            return new KeyValuePair<DicomFile, Stream>(dicomFile, stream);
        }

        private void ValidateResponseStreams(
            IEnumerable<DicomFile> expectedDicomFiles,
            IEnumerable<Stream> responseStreams)
        {
            List<DicomFile> responseDicomFiles = responseStreams.Select(x => DicomFile.Open(x)).ToList();

            Assert.Equal(expectedDicomFiles.Count(), responseDicomFiles.Count);

            foreach (DicomFile expectedDicomFile in expectedDicomFiles)
            {
                DicomFile actualFile = responseDicomFiles.First(x => x.Dataset.ToDicomInstanceIdentifier().Equals(expectedDicomFile.Dataset.ToDicomInstanceIdentifier()));

                // If the same transfer syntax as original, the files should be exactly the same
                if (expectedDicomFile.Dataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
                {
                    var expectedFileArray = DicomFileToByteArray(expectedDicomFile);
                    var actualFileArray = DicomFileToByteArray(actualFile);

                    Assert.Equal(expectedFileArray.Length, actualFileArray.Length);

                    for (var ii = 0; ii < expectedFileArray.Length; ii++)
                    {
                        Assert.Equal(expectedFileArray[ii], actualFileArray[ii]);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private byte[] DicomFileToByteArray(DicomFile dicomFile)
        {
            using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                dicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
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
