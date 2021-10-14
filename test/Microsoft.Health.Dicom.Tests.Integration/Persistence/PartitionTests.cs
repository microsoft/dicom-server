// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class PartitionTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly SqlDataStoreTestsFixture _fixture;

        public PartitionTests(SqlDataStoreTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task WhenInstanceIsCreatedWithNoPartition_Then_DefaultPartitionIsPresent()
        {
            var dicomInstanceIdentifier = await CreateInstance();

            var instanceVersions = await _fixture.InstanceStore.GetInstanceIdentifierAsync(
                dicomInstanceIdentifier.StudyInstanceUid,
                dicomInstanceIdentifier.SeriesInstanceUid,
                dicomInstanceIdentifier.SopInstanceUid);

            var latestVersion = instanceVersions.OrderBy(x => x.Version).Last();

            Assert.Equal(DefaultPartition.Key, latestVersion.PartitionKey);
        }

        private async Task<VersionedInstanceIdentifier> CreateInstance(
            bool instanceFullyCreated = true,
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            var newDataSet = new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, sopInstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
            };

            var version = await _fixture.IndexDataStore.BeginCreateInstanceIndexAsync(newDataSet);

            var versionedIdentifier = newDataSet.ToVersionedInstanceIdentifier(version);

            if (instanceFullyCreated)
            {
                await _fixture.IndexDataStore.EndCreateInstanceIndexAsync(newDataSet, version);
            }

            return versionedIdentifier;
        }
    }
}
