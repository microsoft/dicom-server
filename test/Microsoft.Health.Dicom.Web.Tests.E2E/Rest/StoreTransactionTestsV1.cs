// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class StoreTransactionTestsV1 : StoreTransactionTests
{
    public StoreTransactionTestsV1(HttpIntegrationTestFixture<Startup> fixture) : base(fixture)
    {
    }

    protected override IDicomWebClient GetClient(HttpIntegrationTestFixture<Startup> fixture)
    {
        return fixture.GetDicomWebClient(DicomApiVersions.V1);
    }

    [Fact]
    public async Task GivenDatasetWithInvalidVrValue_WhenStoring_TheServerShouldReturnConflict()
    {
        var studyInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithInvalidVr(studyInstanceUID);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManager.StoreAsync(new[] { dicomFile1 }));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.False(exception.ResponseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

        ValidationHelpers.ValidateFailedSopSequence(
            exception.ResponseDataset,
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.ValidationFailedFailureCode));
    }

    [Fact]
    public async Task GivenInstanceWithPatientIDWithInvalidChars_WhenStoreInstanceWithPartialValidation_ThenExpectDicom100ErrorAndConflictStatus()
    {
        DicomFile dicomFile1 = new DicomFile(
            Samples.CreateRandomInstanceDataset(patientId: "Before Null Character, \0", validateItems: false));

        var ex = await Assert.ThrowsAsync<DicomWebException>(
            () => _instancesManager.StoreAsync(new[] { dicomFile1 }));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        DicomSequence failedSOPSequence = ex.ResponseDataset.GetSequence(DicomTag.FailedSOPSequence);
        DicomSequence failedAttributesSequence = failedSOPSequence.Items[0].GetSequence(DicomTag.FailedAttributesSequence);
        Assert.Equal(
            """DICOM100: (0010,0020) - Dicom element 'PatientID' failed validation for VR 'LO': Value contains invalid character.""",
            failedAttributesSequence.Items[0].GetString(DicomTag.ErrorComment));
    }
}
