// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

        [Fact]
        public async Task GivenValidCustomTags_WhenGoThroughEndToEndScenario_ThenShouldSucceed()
        {
            DicomTag privateTag = new DicomTag(0x0407, 0x0001);
            CustomTagEntry privateTagEntry = new CustomTagEntry { Path = privateTag.GetPath(), VR = DicomVRCode.SS, Level = CustomTagLevel.Instance };
            DicomTag standardTag = DicomTag.ManufacturerModelName;
            CustomTagEntry standardTagEntry = new CustomTagEntry { Path = standardTag.GetPath(), VR = standardTag.GetDefaultVR().Code, Level = CustomTagLevel.Series };
            DicomTag standardTag2 = DicomTag.PatientSex;
            CustomTagEntry standardTag2Entry = new CustomTagEntry { Path = standardTag2.GetPath(), VR = standardTag2.GetDefaultVR().Code, Level = CustomTagLevel.Study };

            CustomTagEntry[] entries = new CustomTagEntry[] { privateTagEntry, standardTagEntry, standardTag2Entry };

            // Create test file
            string studyUid = TestUidGenerator.Generate();
            string seriesUid1 = TestUidGenerator.Generate();
            string seriesUid2 = TestUidGenerator.Generate();
            string instanceUid1 = TestUidGenerator.Generate();
            string instanceUid2 = TestUidGenerator.Generate();
            string instanceUid3 = TestUidGenerator.Generate();
            DicomDataset dataset1 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid1, sopInstanceUid: instanceUid1);
            dataset1.Add(privateTag, 1);
            dataset1.Add(standardTag, "ManufacturerModelName1");
            dataset1.Add(standardTag2, "0");

            DicomDataset dataset2 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid1, sopInstanceUid: instanceUid2);
            dataset2.Add(privateTag, 2);
            dataset2.Add(standardTag, "ManufacturerModelName2");
            dataset2.Add(standardTag2, "0");

            DicomDataset dataset3 = Samples.CreateRandomInstanceDataset(studyInstanceUid: studyUid, seriesInstanceUid: seriesUid2, sopInstanceUid: instanceUid3);
            dataset3.Add(privateTag, 3);
            dataset3.Add(standardTag, "ManufacturerModelName3");
            dataset3.Add(standardTag2, "1");

            await _fixture.Client.AddCustomTagAsync(entries);

            DicomFile[] files = new DicomDataset[] { dataset1, dataset2, dataset3 }.Select(item => new DicomFile(item)).ToArray();

            // store instances
            await _fixture.Client.StoreAsync(files, studyInstanceUid: string.Empty, cancellationToken: default);

            // Query on instance
            DicomWebAsyncEnumerableResponse<DicomDataset> queryInstanceResponse = await _fixture.Client.QueryAsync($"/instances?{privateTag.GetPath()}=3", cancellationToken: default);
            DicomDataset[] instanceResult = await queryInstanceResponse.ToArrayAsync();
            Assert.Single(instanceResult);
            Assert.Equal(instanceUid3, instanceResult[0].GetSingleValue<int>(DicomTag.SOPInstanceUID));

            // Query on series
            DicomWebAsyncEnumerableResponse<DicomDataset> querySeriesResponse = await _fixture.Client.QueryAsync($"/series?{standardTag.GetPath()}=ManufacturerModelName2", cancellationToken: default);
            DicomDataset[] seriesResult = await querySeriesResponse.ToArrayAsync();
            Assert.Single(seriesResult);
            Assert.Equal(seriesUid1, seriesResult[0].GetSingleValue<string>(DicomTag.SeriesInstanceUID));

            // Query on study
            DicomWebAsyncEnumerableResponse<DicomDataset> queryStudyResponse = await _fixture.Client.QueryAsync($"/studies?{standardTag2.GetPath()}=1", cancellationToken: default);
            DicomDataset[] studyResult = await queryStudyResponse.ToArrayAsync();
            Assert.Single(studyResult);
            Assert.Equal(studyUid, seriesResult[0].GetSingleValue<string>(DicomTag.StudyInstanceUID));

            // Delete instance custom tag
            await _fixture.Client.DeleteCustomTagAsync($"/tags/{privateTag.GetPath()}");
            DicomWebAsyncEnumerableResponse<DicomDataset> queryInstanceResponse2 = await _fixture.Client.QueryAsync($"/instances?{privateTag.GetPath()}=3", cancellationToken: default);
            DicomDataset[] instanceResult2 = await queryInstanceResponse2.ToArrayAsync();
            Assert.Empty(instanceResult2);

            // Delete series custom tag
            await _fixture.Client.DeleteCustomTagAsync($"/tags/{standardTag.GetPath()}");
            DicomWebAsyncEnumerableResponse<DicomDataset> querySeriesResponse2 = await _fixture.Client.QueryAsync($"/series?{standardTag.GetPath()}=ManufacturerModelName2", cancellationToken: default);
            DicomDataset[] seriesResult2 = await querySeriesResponse2.ToArrayAsync();
            Assert.Empty(seriesResult2);

            // Delete study custom tag
            DicomWebAsyncEnumerableResponse<DicomDataset> queryStudyResponse2 = await _fixture.Client.QueryAsync($"/studies?{standardTag2.GetPath()}=1", cancellationToken: default);
            DicomDataset[] studyResult2 = await queryStudyResponse2.ToArrayAsync();
            Assert.Empty(studyResult2);
        }
    }
}
