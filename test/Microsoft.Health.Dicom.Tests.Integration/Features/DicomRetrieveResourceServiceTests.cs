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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Integration.Persistence;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class DicomRetrieveResourceServiceTests : IClassFixture<DicomDataStoreTestsFixture>, IClassFixture<DicomSqlDataStoreTestsFixture>
    {
        private readonly DicomRetrieveResourceService _dicomRetrieveResourceService;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomFileStore _dicomFileStore;
        private static readonly CancellationToken _defaultCancellationToken = new CancellationTokenSource().Token;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();

        public DicomRetrieveResourceServiceTests(DicomDataStoreTestsFixture blobStoragefixture, DicomSqlDataStoreTestsFixture sqlIndexStorageFixture)
        {
            _dicomIndexDataStore = sqlIndexStorageFixture.DicomIndexDataStore;
            _dicomInstanceStore = sqlIndexStorageFixture.DicomInstanceStore;
            _dicomFileStore = blobStoragefixture.DicomFileStore;
            _recyclableMemoryStreamManager = blobStoragefixture.RecyclableMemoryStreamManager;
            _dicomRetrieveResourceService = new DicomRetrieveResourceService(_dicomInstanceStore, _dicomFileStore, blobStoragefixture.RecyclableMemoryStreamManager, NullLogger<DicomRetrieveResourceService>.Instance);
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
        {
            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(requestedTransferSyntax: null, _studyInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForStudy_ThenNotFoundIsThrown()
        {
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 1, true);
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 1, false);
            await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(requestedTransferSyntax: null, _studyInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForStudy_ThenInstancesInStudyAreRetrievedSuccesfully()
        {
            List<DicomDataset> datasets = new List<DicomDataset>();
            datasets.AddRange(await GenerateDicomDatasets(_firstSeriesInstanceUid, 2, true));
            datasets.AddRange(await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true));

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(requestedTransferSyntax: null, _studyInstanceUid),
                _defaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            ValidateResponseDicomFiles(response.ResponseStreams, datasets.Select(ds => ds));
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
        {
            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(requestedTransferSyntax: null, _studyInstanceUid, _firstSeriesInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstancesWithMissingFile_WhenRetrieveRequestForSeries_ThenNotFoundIsThrown()
        {
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 2, true);
            await GenerateDicomDatasets(_firstSeriesInstanceUid, 1, false);
            await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(requestedTransferSyntax: null, _studyInstanceUid, _firstSeriesInstanceUid),
                _defaultCancellationToken));
        }

        [Fact]
        public async Task GivenStoredInstances_WhenRetrieveRequestForSeries_ThenInstancesInSeriesAreRetrievedSuccesfully()
        {
            List<DicomDataset> datasets = new List<DicomDataset>();
            datasets.AddRange(await GenerateDicomDatasets(_firstSeriesInstanceUid, 2, true));
            datasets.AddRange(await GenerateDicomDatasets(_secondSeriesInstanceUid, 1, true));

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(requestedTransferSyntax: null, _studyInstanceUid, _firstSeriesInstanceUid),
                _defaultCancellationToken);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            ValidateResponseDicomFiles(response.ResponseStreams, datasets.Select(ds => ds).Where(ds => ds.ToDicomInstanceIdentifier().SeriesInstanceUid == _firstSeriesInstanceUid));
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
            long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dataset);

            VersionedDicomInstanceIdentifier dicomInstanceIdentifier = dataset.ToVersionedDicomInstanceIdentifier(version);

            if (flagToStoreInstance)
            {
                DicomFile dicomFile = new DicomFile(dataset);

                Samples.AppendRandomPixelData(5, 5, 0, dicomFile);

                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    dicomFile.Save(stream);
                    stream.Position = 0;
                    await _dicomFileStore.AddFileAsync(
                        dicomInstanceIdentifier,
                        stream);
                }
            }

            await _dicomIndexDataStore.UpdateInstanceIndexStatusAsync(dicomInstanceIdentifier, DicomIndexStatus.Created);
        }

        private void ValidateResponseDicomFiles(
            IEnumerable<Stream> responseStreams,
            IEnumerable<DicomDataset> expectedDatasets)
        {
            List<DicomFile> responseDicomFiles = responseStreams.Select(x => DicomFile.Open(x)).ToList();

            Assert.Equal(expectedDatasets.Count(), responseDicomFiles.Count);

            foreach (DicomDataset expectedDataset in expectedDatasets)
            {
                DicomFile actualFile = responseDicomFiles.First(x => x.Dataset.ToDicomInstanceIdentifier().Equals(expectedDataset.ToDicomInstanceIdentifier()));

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
