// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
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

        private readonly IDicomWebClient _client;
        private readonly DicomInstancesManager _instancesManager;

        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.GetDicomWebClient();
            _instancesManager = new DicomInstancesManager(_client);
        }

        private async Task<(InstanceIdentifier, DicomFile)> CreateAndStoreDicomFile(int numberOfFrames = 0)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: numberOfFrames);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            await _instancesManager.StoreAsync(new[] { dicomFile });
            return (dicomInstance, dicomFile);
        }

        private InstanceIdentifier RandomizeInstanceIdentifier(DicomDataset dataset)
        {
            InstanceIdentifier newId = new InstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, newId.StudyInstanceUid);
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, newId.SeriesInstanceUid);
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, newId.SopInstanceUid);
            return newId;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _instancesManager.DisposeAsync();
        }

        public static IEnumerable<object[]> GetUnsupportedAcceptHeadersForStudiesAndSeries
        {
            get
            {
                yield return new object[] { true, DicomWebConstants.ApplicationDicomMediaType, DicomWebConstants.OriginalDicomTransferSyntax }; // use single part instead of multiple part
                yield return new object[] { false, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax }; // unsupported media type image/png
                yield return new object[] { false, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100" }; // unsupported media type MPEG2
            }
        }
    }
}
