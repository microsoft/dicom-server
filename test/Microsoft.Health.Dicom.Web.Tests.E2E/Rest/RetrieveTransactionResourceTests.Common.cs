// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    /// <summary>
    /// The tests for retrieving resources.
    /// </summary>
    public partial class RetrieveTransactionResourceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IAsyncLifetime
    {
        private const string TestFileFolder = @"TestFiles\RetrieveTransactionResourceTests\";
        private const string FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder = TestFileFolder + "FromExplicitVRLittleEndianToJPEG2000Lossless";
        private const string FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder = TestFileFolder + "FromJPEG2000LosslessToExplicitVRLittleEndian";
        private const string RequestOriginalContentTestFolder = TestFileFolder + "RequestOriginalContent";
        private const string PrereleaseV1VersionPath = "/v1.0-prerelease";

        private readonly IDicomWebClient _client;
        private readonly HashSet<string> _studiesToClean = new HashSet<string>();

        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
        }

        private async Task<(InstanceIdentifier, DicomFile)> CreateAndStoreDicomFile(int numberOfFrames = 0)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: numberOfFrames);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            await InternalStoreAsync(new[] { dicomFile });
            return (dicomInstance, dicomFile);
        }

        private async Task InternalStoreAsync(IEnumerable<DicomFile> dicomFiles)
        {
            await _client.StoreAsync(dicomFiles);
            foreach (DicomFile dicomFile in dicomFiles)
            {
                _studiesToClean.Add(dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
            }
        }

        private InstanceIdentifier RandomizeInstanceIdentifier(DicomDataset dataset)
        {
            InstanceIdentifier newId = new InstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, newId.StudyInstanceUid);
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, newId.SeriesInstanceUid);
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, newId.SopInstanceUid);
            return newId;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            foreach (string studyUid in _studiesToClean)
            {
                await _client.DeleteStudyAsync(studyUid);
            }

            _studiesToClean.Clear();
        }
    }
}
