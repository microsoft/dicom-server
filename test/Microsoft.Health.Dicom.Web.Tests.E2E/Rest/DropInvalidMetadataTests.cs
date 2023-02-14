// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;


[Trait("Category", "leniency")]
public class DropInvalidMetadataTests : IClassFixture<EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture<Startup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly IDicomWebClient _clientV1;
    private readonly DicomInstancesManager _instancesManager;
    private readonly DicomInstancesManager _instancesManagerV1;
    private readonly string _partition = TestUidGenerator.Generate();

    public DropInvalidMetadataTests(EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture<Startup> fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _client = fixture.GetDicomWebClient(DicomApiVersions.V2);
        _clientV1 = fixture.GetDicomWebClient(DicomApiVersions.V1);
        _instancesManager = new DicomInstancesManager(_client);
        _instancesManagerV1 = new DicomInstancesManager(_clientV1);
        DicomValidationBuilderExtension.SkipValidation(null);
    }

    [Fact]
    public async Task GivenInstanceWithAnInvalidIndexableAttribute_WhenUsingV1WithV2Enabled_TheServerShouldReturnConflict()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "20220315");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManagerV1.StoreAsync(
            new[] { dicomFile },
            partitionName: _partition)
        );

        // assert
        Assert.Equal("Conflict", exception.Message);
        DicomSequence sq = exception.ResponseDataset.GetSequence(DicomTag.FailedSOPSequence);
        DicomDataset instance = sq.Items[0];
        Assert.Equal(
            FailureReasonCodes.ValidationFailure.ToString(CultureInfo.InvariantCulture),
            instance.GetString(DicomTag.FailureReason));
    }


    [Fact]
    public async Task GivenInstanceWithAnInvalidIndexableAttribute_WhenEnableDropInvalidDicomJsonMetadata_ThenInvalidDataDroppedAndValidDataWritten()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "20220315");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(
            new[] { dicomFile },
            partitionName: _partition);

        // assertions

        using DicomWebResponse<DicomFile> retrievedInstance = await _client.RetrieveInstanceAsync(
            dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
            dicomTransferSyntax: "*",
            partitionName: _partition);

        DicomFile retrievedDicomFile = await retrievedInstance.GetValueAsync();

        // expect that valid attribute stored in dicom file
        Assert.Equal(
            dicomFile.Dataset.GetString(DicomTag.PatientBirthDate),
            retrievedDicomFile.Dataset.GetString(DicomTag.PatientBirthDate)
        );

        DicomDataset retrievedMetadata = await ResponseHelper.GetMetadata(_client, dicomFile, _partition);

        // expect valid data stored in metadata/JSON
        retrievedMetadata.GetString(DicomTag.PatientBirthDate);

        // valid searchable index attr was stored, so we can query for instance using the valid attr
        Assert.Single(await GetInstanceByAttribute(dicomFile, DicomTag.PatientBirthDate));

        // expect that metadata invalid date not present
        DicomDataException thrownException = Assert.Throws<DicomDataException>(
            () => retrievedMetadata.GetString(DicomTag.StudyDate));
        Assert.Equal("Tag: (0008,0020) not found in dataset", thrownException.Message);

        // attempting to query with invalid attr produces a BadRequest
        DicomWebException caughtException = await Assert.ThrowsAsync<DicomWebException>(
            async () => await GetInstanceByAttribute(dicomFile, DicomTag.StudyDate));

        Assert.Contains(
            "BadRequest: Invalid query: specified Date value 'NotAValidStudyDate' is invalid for attribute 'StudyDate'" +
            ". Date should be valid and formatted as yyyyMMdd.",
            caughtException.Message);

        // assert on response
        DicomDataset responseDataset = await response.GetValueAsync();
        DicomSequence refSopSequence = responseDataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a comment sequence present
        DicomSequence failedAttributesSequence = firstInstance.GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Single(failedAttributesSequence);

        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            """DICOM100: (0008,0020) - Content "NotAValidStudyDate" does not validate VR DA: one of the date values does not match the pattern YYYYMMDD""",
            failedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment)
        );
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _instancesManager.DisposeAsync();
    }

    private static DicomFile GenerateDicomFile()
    {
        DicomFile dicomFile = Samples.CreateRandomDicomFile(
            studyInstanceUid: TestUidGenerator.Generate(),
            seriesInstanceUid: TestUidGenerator.Generate(),
            sopInstanceUid: TestUidGenerator.Generate()
        );
        return dicomFile;
    }

    private async Task<IEnumerable<DicomDataset>> GetInstanceByAttribute(DicomFile dicomFile, DicomTag searchTag)
    {
        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryInstancesAsync(
            $"{searchTag.DictionaryEntry.Keyword}={dicomFile.Dataset.GetString(searchTag)}",
            partitionName: _partition
        );
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        DicomDataset[] datasets = await response.ToArrayAsync();

        IEnumerable<DicomDataset> matchedInstances = datasets.Where(
            ds =>
                ds.GetString(DicomTag.StudyInstanceUID) == dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
        return matchedInstances;
    }

}
