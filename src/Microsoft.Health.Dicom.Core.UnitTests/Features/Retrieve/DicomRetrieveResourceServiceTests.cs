// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
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
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(x)).Result));

            _dicomFileStore.GetFileAsync(instanceIdentifiers.Last(), _defaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest("*", _studyInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenIsSuccessful_ThenInstancesInStudyAreRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);

            instanceIdentifiers.Select(x => _dicomFileStore.GetFileAsync(x, _defaultCancellationToken).Returns(
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(x)).Result));

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid),
                   _defaultCancellationToken);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
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
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(x)).Result));

            _dicomFileStore.GetFileAsync(instanceIdentifiers.Last(), _defaultCancellationToken).Throws(new DicomDataStoreException(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenIsSuccessful_ThenInstancesInSeriesAreRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);

            instanceIdentifiers.Select(x => _dicomFileStore.GetFileAsync(x, _defaultCancellationToken).Returns(
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(x)).Result));

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid),
                   _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveRequestForInstance_WhenIsSuccessful_ThenInstanceIsRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);

            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First())).Result);

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid),
                   _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GivenRetrieveRequestForFramesInInstance_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);

            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First())).Result);

            await Assert.ThrowsAsync<DicomFrameNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new List<int> { 1, 2 }),
                   _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForFramesInInstance_WhenFailsToRetrieveOne_ThenNotFoundIsThrown()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);

            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First()), 3).Result);

            await Assert.ThrowsAsync<DicomFrameNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new List<int> { 1, 4 }),
                   _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForFramesInInstance_WhenIsSuccessful_ThenFramesInInstanceAreRetrievedSuccesfully()
        {
            List<VersionedDicomInstanceIdentifier> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Frames);

            _dicomFileStore.GetFileAsync(instanceIdentifiers.First(), _defaultCancellationToken).Returns(
                StreamsOfStoredFilesFromDatasets(GenerateDatasetsFromIdentifiers(instanceIdentifiers.First()), 3).Result);

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                   new DicomRetrieveResourceRequest("*", _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, new List<int> { 1, 2 }),
                   _defaultCancellationToken);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
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

        private async Task<Stream> StreamsOfStoredFilesFromDatasets(DicomDataset dataset, int frames = 0)
        {
            DicomFile dicomFile = new DicomFile(dataset);
            Samples.AppendRandomPixelData(5, 5, frames, dicomFile);

            MemoryStream stream = new MemoryStream();
            await dicomFile.SaveAsync(stream);
            stream.Position = 0;

            return stream;
        }
    }
}
