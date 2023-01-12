// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;


[Trait("Category", "bvt-dp")]
public class DropInvalidMetadataTests : IClassFixture<EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture<Startup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly DicomInstancesManager _instancesManager;

    public DropInvalidMetadataTests(EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture<Startup> fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _client = fixture.GetDicomWebClient();
        _instancesManager = new DicomInstancesManager(_client);
        DicomValidationBuilderExtension.SkipValidation(null);
    }

    [Fact]
    public async Task GivenInstanceWithAnInvalidIndexableAttribute_WhenEnableDropInvalidDicomJsonMetadata_ThenValidDataStillWritten()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "20220315");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        await _instancesManager.StoreAsync(new[] { dicomFile });

        // assert

        using DicomWebResponse<DicomFile> retrievedInstance = await _client.RetrieveInstanceAsync(
            dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
            dicomTransferSyntax: "*");

        DicomFile retrievedDicomFile = await retrievedInstance.GetValueAsync();

        // expect that valid attribute stored in dicom file
        Assert.Equal(
            dicomFile.Dataset.GetString(DicomTag.PatientBirthDate),
            retrievedDicomFile.Dataset.GetString(DicomTag.PatientBirthDate)
        );

        DicomDataset retrievedMetadata = await ResponseHelper.GetMetadata(_client, dicomFile);

        // expect valid data stored in metadata/JSON
        retrievedMetadata.GetString(DicomTag.PatientBirthDate);

        // valid searchable index attr was stored, so we can query for instance using the valid attr
        Assert.Single(await GetInstanceByAttribute(dicomFile, DicomTag.PatientBirthDate));
    }

    private async Task<IEnumerable<DicomDataset>> GetInstanceByAttribute(DicomFile dicomFile, DicomTag searchTag)
    {
        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryInstancesAsync(
            $"{searchTag.DictionaryEntry.Keyword}={dicomFile.Dataset.GetString(searchTag)}"
        );
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        DicomDataset[] datasets = await response.ToArrayAsync();

        IEnumerable<DicomDataset> matchedInstances = datasets.Where(
            ds =>
                ds.GetString(DicomTag.StudyInstanceUID) == dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
        return matchedInstances;
    }

    [Fact]
    public async Task GivenInstanceWithAnInvalidIndexableAttribute_WhenEnableDropInvalidDicomJsonMetadata_ThenInvalidDataDropped()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "20220315");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        await _instancesManager.StoreAsync(new[] { dicomFile });

        // assert

        using DicomWebResponse<DicomFile> retrievedInstance = await _client.RetrieveInstanceAsync(
            dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
            dicomTransferSyntax: "*");

        DicomFile retrievedDicomFile = await retrievedInstance.GetValueAsync();

        // expect that invalid attribute stored in dicom file
        Assert.Equal(
            dicomFile.Dataset.GetString(DicomTag.StudyDate),
            retrievedDicomFile.Dataset.GetString(DicomTag.StudyDate)
            );

        DicomDataset retrievedMetadata = await ResponseHelper.GetMetadata(_client, dicomFile);

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
    }

    [Fact]
    public async Task GivenInstanceWithAnInvalidIndexableAttribute_WhenEnableDropInvalidDicomJsonMetadata_ThenExpectASingleCommentsSequenceInResponse()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "20220315");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile });
        DicomDataset responseDataset = await response.GetValueAsync();

        // assert
        DicomSequence refSopSequence = responseDataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a comment sequence present
        DicomSequence commentSequence = firstInstance.GetSequence(DicomTag.CalculationCommentSequence);
        Assert.Single(commentSequence);

        // expect comment sequence has single warning about single invalid attribute
        Assert.Equal(
            "Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag.QueryTag - Dicom element 'StudyDate' failed validation for VR 'DA': Value cannot be parsed as a valid date.",
            commentSequence.Items[0].GetString(DicomTag.ErrorComment)
            );
    }

    [Fact]
    public async Task GivenInstanceWithMultipleInvalidIndexableAttributes_WhenEnableDropInvalidDicomJsonMetadata_ThenExpectMultipleErrorCommentsInASingleCommentsSequenceInResponse()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "NotAValidStudyDate");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile });
        DicomDataset responseDataset = await response.GetValueAsync();

        // assert
        DicomSequence refSopSequence = responseDataset.GetSequence(DicomTag.ReferencedSOPSequence);
        Assert.Single(refSopSequence);

        DicomDataset firstInstance = refSopSequence.Items[0];

        // expect a comment sequence present
        DicomSequence commentSequence = firstInstance.GetSequence(DicomTag.CalculationCommentSequence);

        // expect comment sequence has same count of warnings as invalid attributes
        Assert.Equal(2, commentSequence.Items.Count);

        // expect that the two invalid attributes are represented in warnings
        Assert.Equal(
            "Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag.QueryTag - Dicom element 'StudyDate' failed validation for VR 'DA': Value cannot be parsed as a valid date.",
            commentSequence.Items[0].GetString(DicomTag.ErrorComment));

        // expect that the two invalid attributes are represented in warnings
        Assert.Equal(
            "Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag.QueryTag - Dicom element 'PatientBirthDate' failed validation for VR 'DA': Value cannot be parsed as a valid date.",
            commentSequence.Items[1].GetString(DicomTag.ErrorComment)
        );
    }

    [Fact]
    public async Task GivenMultipleInstancesWithInvalidIndexableAttributes_WhenEnableDropInvalidDicomJsonMetadata_ThenExpectMultipleCommentsSequencesInResponse()
    {
        // setup
        DicomFile dicomFile1 = GenerateDicomFile();

        DicomFile dicomFile2 = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");

        dicomFile1.Dataset.Add(dicomDataset);
        dicomFile2.Dataset.Add(dicomDataset);

        // run
        DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile1, dicomFile2 });
        DicomDataset responseDataset = await response.GetValueAsync();

        // assert
        DicomSequence refSopSequence = responseDataset.GetSequence(DicomTag.ReferencedSOPSequence);

        // assert the refSopSequence has two instances being represented
        Assert.Equal(2, refSopSequence.Items.Count);

        foreach (DicomDataset instance in refSopSequence.Items)
        {
            // expect a comment sequence present
            DicomSequence commentSequence = instance.GetSequence(DicomTag.CalculationCommentSequence);
            Assert.Single(commentSequence);

            // expect comment sequence has same count of warnings as invalid attributes
            Assert.Single(commentSequence);

            // expect that the two invalid attribute is represented in warnings
            Assert.Equal(
                "Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag.QueryTag - Dicom element 'StudyDate' failed validation for VR 'DA': Value cannot be parsed as a valid date.",
                commentSequence.Items[0].GetString(DicomTag.ErrorComment));
        }
    }

    [Fact]
    public async Task GivenInstanceWithAnInvalidIndexableAttribute_WhenEnableDropInvalidDicomJsonMetadata_ThenExpectValidReferencedSopSequenceInResponse()
    {
        // setup
        DicomFile dicomFile = GenerateDicomFile();

        DicomDataset dicomDataset = new DicomDataset().NotValidated();

        dicomDataset.Add(DicomTag.StudyDate, "NotAValidStudyDate");
        dicomDataset.Add(DicomTag.PatientBirthDate, "20220315");

        dicomFile.Dataset.Add(dicomDataset);

        // run
        DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile });
        DicomDataset responseDataset = await response.GetValueAsync();

        // assert
        await ResponseHelper.
            ValidateReferencedSopSequenceAsync(
            response,
            ResponseHelper.ConvertToReferencedSopSequenceEntry(
                _client,
                dicomFile.Dataset
                )
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

}
