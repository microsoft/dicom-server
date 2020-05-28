// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ChangeFeedTests : IClassFixture<ChangeFeedTestsFixture>
    {
        private readonly ChangeFeedTestsFixture _fixture;

        public ChangeFeedTests(ChangeFeedTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GivenInstance_WhenAddedAndDeletedAndAdded_ChangeFeedEntryAvailable()
        {
            // create and validate
            var dicomInstanceIdentifier = await CreateValidInstance();
            await ValidateInsertFeed(dicomInstanceIdentifier, 1);

            // delete and validate
            await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
            await ValidateDeleteFeed(dicomInstanceIdentifier, 2);

            // re-create the same instance and validate
            await CreateValidInstance(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid);
            await ValidateInsertFeed(dicomInstanceIdentifier, 3);
        }

        private async Task ValidateInsertFeed(VersionedInstanceIdentifier dicomInstanceIdentifier, int expectedCount)
        {
            IReadOnlyList<ChangeFeedRow> result = await _fixture.DicomIndexDataStoreTestHelper.GetChangeFeedRowsAsync(
                dicomInstanceIdentifier.StudyInstanceUid,
                dicomInstanceIdentifier.SeriesInstanceUid,
                dicomInstanceIdentifier.SopInstanceUid);

            Assert.NotNull(result);
            Assert.Equal(expectedCount, result.Count);
            Assert.Equal((int)ChangeFeedAction.Create, result.Last().Action);
            Assert.Equal(result.Last().OriginalWatermark, result.Last().CurrentWatermark);

            int i = 0;
            while (i < expectedCount - 1)
            {
                ChangeFeedRow r = result[i];
                Assert.NotEqual(r.OriginalWatermark, r.CurrentWatermark);
                i++;
            }
        }

        private async Task ValidateDeleteFeed(VersionedInstanceIdentifier dicomInstanceIdentifier, int expectedCount)
        {
            IReadOnlyList<ChangeFeedRow> result = await _fixture.DicomIndexDataStoreTestHelper.GetChangeFeedRowsAsync(
                dicomInstanceIdentifier.StudyInstanceUid,
                dicomInstanceIdentifier.SeriesInstanceUid,
                dicomInstanceIdentifier.SopInstanceUid);

            Assert.NotNull(result);
            Assert.Equal(expectedCount, result.Count);
            Assert.Equal((int)ChangeFeedAction.Delete, result.Last().Action);

            foreach (ChangeFeedRow row in result)
            {
                Assert.Null(row.CurrentWatermark);
            }
        }

        private async Task<VersionedInstanceIdentifier> CreateValidInstance(
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

            var version = await _fixture.DicomIndexDataStore.CreateInstanceIndexAsync(newDataSet);

            var versionedIdentifier = newDataSet.ToVersionedInstanceIdentifier(version);

            await _fixture.DicomIndexDataStore.UpdateInstanceIndexStatusAsync(versionedIdentifier, Core.Models.IndexStatus.Created);

            return versionedIdentifier;
        }
    }
}
