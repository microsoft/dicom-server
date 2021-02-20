// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class CustomTagTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly HttpIntegrationTestFixture<Startup> _fixture;

        public CustomTagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _fixture = fixture;
        }

        [Fact(Skip = "Feature Not Ready")]
        public async Task GivenValidCustomTags_WhenGoThroughEndToEndScenario_ThenShouldSucceed()
        {
            // Prepare 3 custom tags.
            // One is private tag on Instance level
            DicomTag privateTag = new DicomTag(0x0407, 0x0001);
            CustomTagEntry privateTagEntry = new CustomTagEntry { Path = privateTag.GetPath(), VR = DicomVRCode.SS, Level = CustomTagLevel.Instance };

            // One is standard tag on Series level
            DicomTag standardTagSeries = DicomTag.ManufacturerModelName;
            CustomTagEntry standardTagSeriesEntry = new CustomTagEntry { Path = standardTagSeries.GetPath(), VR = standardTagSeries.GetDefaultVR().Code, Level = CustomTagLevel.Series };

            // One is standard tag on Study level
            DicomTag standardTagStudy = DicomTag.PatientSex;
            CustomTagEntry standardTagStudyEntry = new CustomTagEntry { Path = standardTagStudy.GetPath(), VR = standardTagStudy.GetDefaultVR().Code, Level = CustomTagLevel.Study };

            CustomTagEntry[] entries = new CustomTagEntry[] { privateTagEntry, standardTagSeriesEntry, standardTagStudyEntry };

            // Create 3 test files on same studyUid.
            string studyUid = TestUidGenerator.Generate();
            string seriesUid1 = TestUidGenerator.Generate();
            string seriesUid2 = TestUidGenerator.Generate();
            string instanceUid1 = TestUidGenerator.Generate();
            string instanceUid2 = TestUidGenerator.Generate();
            string instanceUid3 = TestUidGenerator.Generate();

            // One is on seriesUid1 and instanceUid1
            DicomDataset dataset1 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid1, sopInstanceUid: instanceUid1);
            dataset1.Add(privateTag, 1);
            dataset1.Add(standardTagSeries, "ManufacturerModelName1");
            dataset1.Add(standardTagStudy, "0");

            // One is on seriesUid1 and instanceUid2
            DicomDataset dataset2 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid1, sopInstanceUid: instanceUid2);
            dataset2.Add(privateTag, 2);
            dataset2.Add(standardTagSeries, "ManufacturerModelName2");
            dataset2.Add(standardTagStudy, "0");

            // One is on seriesUid2 and instanceUid3
            DicomDataset dataset3 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid2, sopInstanceUid: instanceUid3);
            dataset3.Add(privateTag, 3);
            dataset3.Add(standardTagSeries, "ManufacturerModelName3");
            dataset3.Add(standardTagStudy, "1");

            // Add custom tags
            await _fixture.Client.AddCustomTagAsync(entries);

            // Upload test files
            IEnumerable<DicomFile> dicomFiles = new DicomDataset[] { dataset1, dataset2, dataset3 }.Select(dataset => new DicomFile(dataset));
            await _fixture.Client.StoreAsync(dicomFiles, studyInstanceUid: string.Empty, cancellationToken: default);

            // Query on instance for private tag
            DicomWebAsyncEnumerableResponse<DicomDataset> queryInstanceResponse = await _fixture.Client.QueryAsync($"/instances?{privateTag.GetPath()}=3", cancellationToken: default);
            DicomDataset[] instanceResult = await queryInstanceResponse.ToArrayAsync();
            Assert.Single(instanceResult);
            Assert.Equal(instanceUid3, instanceResult[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            // Query on series for standardTagSeries
            DicomWebAsyncEnumerableResponse<DicomDataset> querySeriesResponse = await _fixture.Client.QueryAsync($"/series?{standardTagSeries.GetPath()}=ManufacturerModelName2", cancellationToken: default);
            DicomDataset[] seriesResult = await querySeriesResponse.ToArrayAsync();
            Assert.Single(seriesResult);
            Assert.Equal(seriesUid1, seriesResult[0].GetSingleValue<string>(DicomTag.SeriesInstanceUID));

            // Query on study for standardTagStudy
            DicomWebAsyncEnumerableResponse<DicomDataset> queryStudyResponse = await _fixture.Client.QueryAsync($"/studies?{standardTagStudy.GetPath()}=1", cancellationToken: default);
            DicomDataset[] studyResult = await queryStudyResponse.ToArrayAsync();
            Assert.Single(studyResult);
            Assert.Equal(studyUid, seriesResult[0].GetSingleValue<string>(DicomTag.StudyInstanceUID));

            // Delete private tag
            await _fixture.Client.DeleteCustomTagAsync(privateTag.GetPath());
            DicomWebAsyncEnumerableResponse<DicomDataset> queryInstanceResponse2 = await _fixture.Client.QueryAsync($"/instances?{privateTag.GetPath()}=3", cancellationToken: default);
            DicomDataset[] instanceResult2 = await queryInstanceResponse2.ToArrayAsync();
            Assert.Empty(instanceResult2);

            // Delete standardTagSeries
            await _fixture.Client.DeleteCustomTagAsync(standardTagSeries.GetPath());
            DicomWebAsyncEnumerableResponse<DicomDataset> querySeriesResponse2 = await _fixture.Client.QueryAsync($"/series?{standardTagSeries.GetPath()}=ManufacturerModelName2", cancellationToken: default);
            DicomDataset[] seriesResult2 = await querySeriesResponse2.ToArrayAsync();
            Assert.Empty(seriesResult2);

            // Delete standardTagStudy
            await _fixture.Client.DeleteCustomTagAsync(standardTagStudy.GetPath());
            DicomWebAsyncEnumerableResponse<DicomDataset> queryStudyResponse2 = await _fixture.Client.QueryAsync($"/studies?{standardTagStudy.GetPath()}=1", cancellationToken: default);
            DicomDataset[] studyResult2 = await queryStudyResponse2.ToArrayAsync();
            Assert.Empty(studyResult2);
        }
    }
}
