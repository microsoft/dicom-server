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
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DicomRetrieveResourceService> _logger;
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private readonly string studyInstanceUid = TestUidGenerator.Generate();
        private readonly string firstSeriesInstanceUid = TestUidGenerator.Generate();
        private readonly string secondSeriesInstanceUid = TestUidGenerator.Generate();

        public DicomRetrieveResourceServiceTests(DicomDataStoreTestsFixture blobStoragefixture, DicomSqlDataStoreTestsFixture sqlIndexStorageFixture)
        {
            _dicomIndexDataStore = sqlIndexStorageFixture.DicomIndexDataStore;
            _dicomInstanceStore = sqlIndexStorageFixture.DicomInstanceStore;
            _dicomFileStore = blobStoragefixture.DicomFileStore;
            _logger = NullLogger<DicomRetrieveResourceService>.Instance;
            _recyclableMemoryStreamManager = blobStoragefixture.RecyclableMemoryStreamManager;
            _dicomRetrieveResourceService = new DicomRetrieveResourceService(_dicomInstanceStore, _dicomFileStore, blobStoragefixture.RecyclableMemoryStreamManager, _logger);
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(null, studyInstanceUid),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenFailsToRetrieveOne_ThenNotFoundIsThrown()
        {
            List<KeyValuePair<DicomDataset, bool>> datasets = await GenerateDicomDatasets(new List<Tuple<string, int, bool>>()
            {
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 1, true),
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 1, false),
                new Tuple<string, int, bool>(secondSeriesInstanceUid, 1, true),
            });

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(null, studyInstanceUid),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForStudy_WhenIsSuccessful_ThenInstancesInStudyAreRetrievedSuccesfully()
        {
            List<KeyValuePair<DicomDataset, bool>> datasets = await GenerateDicomDatasets(new List<Tuple<string, int, bool>>()
            {
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 2, true),
                new Tuple<string, int, bool>(secondSeriesInstanceUid, 1, true),
            });

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(null, studyInstanceUid),
                DefaultCancellationToken);

            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            ValidateResponseDicomFiles(response.ResponseStreams, datasets.Select(ds => ds.Key));
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenFailsToRetrieveAll_ThenNotFoundIsThrown()
        {
            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(null, studyInstanceUid, firstSeriesInstanceUid),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenFailsToRetrieveOne_ThenNotFoundIsThrown()
        {
            List<KeyValuePair<DicomDataset, bool>> datasets = await GenerateDicomDatasets(new List<Tuple<string, int, bool>>()
            {
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 1, true),
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 1, false),
            });

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(() => _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(null, studyInstanceUid, firstSeriesInstanceUid),
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenRetrieveRequestForSeries_WhenIsSuccessful_ThenInstancesInSeriesAreRetrievedSuccesfully()
        {
            List<KeyValuePair<DicomDataset, bool>> datasets = await GenerateDicomDatasets(new List<Tuple<string, int, bool>>()
            {
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 1, true),
                new Tuple<string, int, bool>(firstSeriesInstanceUid, 1, true),
            });

            DicomRetrieveResourceResponse response = await _dicomRetrieveResourceService.GetInstanceResourceAsync(
                new DicomRetrieveResourceRequest(null, studyInstanceUid, firstSeriesInstanceUid),
                DefaultCancellationToken);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

            ValidateResponseDicomFiles(response.ResponseStreams, datasets.Select(ds => ds.Key));
        }

        private async Task<List<KeyValuePair<DicomDataset, bool>>> GenerateDicomDatasets(List<Tuple<string, int, bool>> instancesToSetupPerStudyWithIndicatorToStore)
        {
            List<KeyValuePair<DicomDataset, bool>> dicomDatasets = new List<KeyValuePair<DicomDataset, bool>>();

            foreach (Tuple<string, int, bool> instancesPerStudy in instancesToSetupPerStudyWithIndicatorToStore)
            {
                for (int i = 0; i < instancesPerStudy.Item2; i++)
                {
                    var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                    {
                        { DicomTag.StudyInstanceUID, studyInstanceUid },
                        { DicomTag.SeriesInstanceUID, instancesPerStudy.Item1 },
                        { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                        { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
                        { DicomTag.PatientID, TestUidGenerator.Generate() },
                        { DicomTag.BitsAllocated, (ushort)8 },
                        { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
                    };

                    await StoreDatasetsAndInstances(ds, instancesPerStudy.Item3);

                    dicomDatasets.Add(new KeyValuePair<DicomDataset, bool>(ds, instancesPerStudy.Item3));
                }
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
    }
}
