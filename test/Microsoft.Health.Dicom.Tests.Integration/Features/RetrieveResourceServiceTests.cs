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
using EnsureThat;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Integration.Persistence;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class RetrieveResourceServiceTests : IClassFixture<DataStoreTestsFixture>, IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly RetrieveResourceService _retrieveResourceService;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IInstanceStore _instanceStore;
        private readonly IFileStore _fileStore;
        private readonly ITranscoder _retrieveTranscoder;
        private readonly IFrameHandler _frameHandler;
        private readonly IRetrieveTransferSyntaxHandler _retrieveTransferSyntaxHandler;
        private static readonly CancellationToken _defaultCancellationToken = new CancellationTokenSource().Token;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();

        public RetrieveResourceServiceTests(DataStoreTestsFixture blobStorageFixture, SqlDataStoreTestsFixture sqlIndexStorageFixture)
        {
            EnsureArg.IsNotNull(sqlIndexStorageFixture, nameof(sqlIndexStorageFixture));
            EnsureArg.IsNotNull(blobStorageFixture, nameof(blobStorageFixture));
            _indexDataStore = sqlIndexStorageFixture.IndexDataStore;
            _instanceStore = sqlIndexStorageFixture.InstanceStore;
            _fileStore = blobStorageFixture.FileStore;
            _retrieveTranscoder = Substitute.For<ITranscoder>();
            _frameHandler = Substitute.For<IFrameHandler>();
            _retrieveTransferSyntaxHandler = new RetrieveTransferSyntaxHandler(NullLogger<RetrieveTransferSyntaxHandler>.Instance);
            _recyclableMemoryStreamManager = blobStorageFixture.RecyclableMemoryStreamManager;
            _retrieveResourceService = new RetrieveResourceService(
                _instanceStore, _fileStore, _retrieveTranscoder, _frameHandler, _retrieveTransferSyntaxHandler, blobStorageFixture.RecyclableMemoryStreamManager, NullLogger<RetrieveResourceService>.Instance);
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
        {
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
        {
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 1, true);
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 1, false);
            await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true);

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForStudy_ThenInstancesInStudyAreRetrievedSuccesfully()
        {
            List<DicomDataset> datasets = new List<DicomDataset>();
            datasets.AddRange(await GenerateDicomDatasets(_firstSeriesInstanceUid, 2, true));
            datasets.AddRange(await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true));

            RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
                _defaultCancellationToken);

            ValidateResponseDicomFiles(response.ResponseStreams, datasets.Select(ds => ds));
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
        {
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
        {
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 2, true);
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 1, false);
            await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true);

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForSeries_ThenInstancesInSeriesAreRetrievedSuccesfully()
        {
            List<DicomDataset> datasets = new List<DicomDataset>();
            datasets.AddRange(await GenerateDicomDatasets(_firstSeriesInstanceUid, 2, true));
            datasets.AddRange(await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true));

            RetrieveResourceResponse response = await _retrieveResourceService.GetInstanceResourceAsync(
                new RetrieveResourceRequest(_studyInstanceUid, _firstSeriesInstanceUid, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries() }),
                _defaultCancellationToken);

            ValidateResponseDicomFiles(response.ResponseStreams, datasets.Select(ds => ds).Where(ds => ds.ToInstanceIdentifier().SeriesInstanceUid == _firstSeriesInstanceUid));
        }

        private async Task<List<DicomDataset>> GenerateDicomDatasets(string seriesInstanceUid, int instancesinSeries, bool storeInstanceFile)
        {
            List<DicomDataset> dicomDatasets = new List<DicomDataset>();
            for (int i = 0; i < instancesinSeries; i++)
            {
                var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                {
                    { DicomTag.StudyInstanceUID, _studyInstanceUid },
                    { DicomTag.SeriesInstanceUID, seriesInstanceUid },
                    { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                    { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
                    { DicomTag.PatientID, TestUidGenerator.Generate() },
                    { DicomTag.BitsAllocated, (ushort)8 },
                    { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
                };

                await StoreDatasetsAndInstances(ds, storeInstanceFile);
                dicomDatasets.Add(ds);
            }

            return dicomDatasets;
        }

        private async Task StoreDatasetsAndInstances(DicomDataset dataset, bool flagToStoreInstance)
        {
            long version = await _indexDataStore.CreateInstanceIndexAsync(dataset, new List<CustomTagStoreEntry>());

            VersionedInstanceIdentifier versionedInstanceIdentifier = dataset.ToVersionedInstanceIdentifier(version);

            if (flagToStoreInstance)
            {
                DicomFile dicomFile = new DicomFile(dataset);

                Samples.AppendRandomPixelData(5, 5, 0, dicomFile);

                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    dicomFile.Save(stream);
                    stream.Position = 0;
                    await _fileStore.StoreFileAsync(
                        versionedInstanceIdentifier,
                        stream);
                }
            }

            await _indexDataStore.UpdateInstanceIndexStatusAsync(versionedInstanceIdentifier, IndexStatus.Created);
        }

        private void ValidateResponseDicomFiles(
            IEnumerable<Stream> responseStreams,
            IEnumerable<DicomDataset> expectedDatasets)
        {
            List<DicomFile> responseDicomFiles = responseStreams.Select(x => DicomFile.Open(x)).ToList();

            Assert.Equal(expectedDatasets.Count(), responseDicomFiles.Count);

            foreach (DicomDataset expectedDataset in expectedDatasets)
            {
                DicomFile actualFile = responseDicomFiles.First(x => x.Dataset.ToInstanceIdentifier().Equals(expectedDataset.ToInstanceIdentifier()));

                // If the same transfer syntax as original, the files should be exactly the same
                if (expectedDataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
                {
                    var expectedFileArray = DicomFileToByteArray(new DicomFile(expectedDataset));
                    var actualFileArray = DicomFileToByteArray(actualFile);

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

        private byte[] DicomFileToByteArray(DicomFile dicomFile)
        {
            using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                dicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
