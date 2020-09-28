// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Dicom;
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
    public partial class RetrieveTransactionResourceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IDisposable
    {
        private const string TestFileFolder = @"TestFiles\RetrieveTransactionResourceTests\";
        private const string FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder = TestFileFolder + "FromExplicitVRLittleEndianToJPEG2000Lossless";
        private const string FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder = TestFileFolder + "FromJPEG2000LosslessToExplicitVRLittleEndian";
        private const string RequestOriginalContentTestFolder = TestFileFolder + "RequestOriginalContent";

        private readonly IDicomWebClient _client;
        private HashSet<string> _studiesToClean = new HashSet<string>();

        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        void IDisposable.Dispose()
        {
            // xunit does not seem to call IAsyncDispose.DisposeAsync()
            // Also wait should be okay in a test context
            foreach (string studyUid in _studiesToClean)
            {
                _client.DeleteStudyAsync(studyUid).Wait();
            }

            _studiesToClean.Clear();
        }

        private async Task<(InstanceIdentifier, DicomFile)> CreateAndStoreDicomFile(int numberOfFrames = 0)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: numberOfFrames);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            await InternalStoreAsync(new[] { dicomFile });
            return (dicomInstance, dicomFile);
        }

        private async Task EnsureFileIsStoredAsync(DicomFile dicomFile)
        {
            var instanceId = dicomFile.Dataset.ToInstanceIdentifier();
            DicomWebResponse<IEnumerable<DicomDataset>> tryQuery = await _client.QueryAsync(
                 $"/studies/{instanceId.StudyInstanceUid}/series/{instanceId.SeriesInstanceUid}/instances?SOPInstanceUID={instanceId.SopInstanceUid}");

            if (tryQuery.StatusCode == HttpStatusCode.OK)
            {
                await _client.DeleteStudyAsync(instanceId.StudyInstanceUid);
            }

            await InternalStoreAsync(new[] { dicomFile });
        }

        private async Task InternalStoreAsync(IEnumerable<DicomFile> dicomFiles)
        {
            await _client.StoreAsync(dicomFiles);
            foreach (DicomFile dicomFile in dicomFiles)
            {
                _studiesToClean.Add(dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
            }
        }
    }
}
