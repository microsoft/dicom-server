// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading;
using Dicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class PatientPipelineStepTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private const string DefaultPatientId = "p1";
        private static readonly DicomDataset DefaultDicomDataset = new DicomDataset()
        {
            { DicomTag.PatientID, DefaultPatientId },
        };

        private readonly IFhirService _fhirService = Substitute.For<IFhirService>();
        private readonly IPatientSynchronizer _patientSynchronizer = Substitute.For<IPatientSynchronizer>();
        private readonly PatientPipelineStep _patientPipeline;

        public PatientPipelineStepTests()
        {
            _patientPipeline = new PatientPipelineStep(_fhirService, _patientSynchronizer);
        }

        [Fact]
        public async Task GivenNullMetadata_WhenRequestIsPrepared_ThenItShouldNotCreateEntryComponent()
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            await _patientPipeline.PrepareRequestAsync(context, DefaultCancellationToken);

            Assert.Null(context.Request.Patient);
        }

        [Fact]
        public async Task GivenMissingPatientId_WhenPreparingTheRequest_ThenMissingRequiredDicomTagExceptionShouldBeThrown()
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate(metadata: new DicomDataset()));

            await Assert.ThrowsAsync<MissingRequiredDicomTagException>(() => _patientPipeline.PrepareRequestAsync(context, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenNoExistingPatient_WhenRequestIsPrepared_ThenCorrectEntryComponentShouldBeCreated()
        {
            FhirTransactionContext context = CreateFhirTransactionContext();

            Patient creatingPatient = null;

            _patientSynchronizer.When(synchronizer => synchronizer.Synchronize(DefaultDicomDataset, Arg.Any<Patient>(), isNewPatient: true)).Do(callback =>
            {
                creatingPatient = callback.ArgAt<Patient>(1);

                // Modify a property of patient so changes can be detected.
                creatingPatient.BirthDateElement = new Date(1990, 01, 01);
            });

            await _patientPipeline.PrepareRequestAsync(context, DefaultCancellationToken);

            FhirTransactionRequestEntry actualPatientEntry = context.Request.Patient;

            ValidationUtility.ValidateRequestEntryMinimumRequirementForWithChange(FhirTransactionRequestMode.Create, "Patient", Bundle.HTTPVerb.POST, actualPatientEntry);

            Assert.Equal("identifier=|p1", actualPatientEntry.Request.IfNoneExist);

            Patient actualPatient = Assert.IsType<Patient>(actualPatientEntry.Resource);

            Assert.Collection(
                actualPatient.Identifier,
                identifier => ValidationUtility.ValidateIdentifier(string.Empty, DefaultPatientId, identifier));

            Assert.Equal(creatingPatient.BirthDate, actualPatient.BirthDate);
        }

        [Fact]
        public async Task GivenExistingPatientAndHasChange_WhenRequestIsPrepared_ThenCorrectEntryComponentShouldBeCreated()
        {
            FhirTransactionContext context = CreateFhirTransactionContext();

            var patient = new Patient()
            {
                Id = "patient1",
                Meta = new Meta()
                {
                    VersionId = "v1",
                },
            };

            _fhirService.RetrievePatientAsync(Arg.Is(TestUtility.BuildIdentifierPredicate(string.Empty, DefaultPatientId)), DefaultCancellationToken)
                .Returns(patient);

            Patient updatingPatient = null;

            _patientSynchronizer.When(synchronizer => synchronizer.Synchronize(DefaultDicomDataset, Arg.Any<Patient>(), isNewPatient: false)).Do(callback =>
            {
                updatingPatient = callback.ArgAt<Patient>(1);

                // Modify a property of patient so changes can be detected.
                updatingPatient.Gender = AdministrativeGender.Other;
            });

            await _patientPipeline.PrepareRequestAsync(context, DefaultCancellationToken);

            FhirTransactionRequestEntry actualPatientEntry = context.Request.Patient;

            ValidationUtility.ValidateRequestEntryMinimumRequirementForWithChange(FhirTransactionRequestMode.Update, "Patient/patient1", Bundle.HTTPVerb.PUT, actualPatientEntry);

            Assert.Equal("W/\"v1\"", actualPatientEntry.Request.IfMatch);
            Assert.Same(updatingPatient, actualPatientEntry.Resource);
        }

        [Fact]
        public async Task GivenExistingPatientAndNoChange_WhenRequestIsPrepared_ThenCorrectEntryComponentShouldBeCreated()
        {
            FhirTransactionContext context = CreateFhirTransactionContext();

            var patient = new Patient()
            {
                Id = "patient1",
                Meta = new Meta()
                {
                    VersionId = "v1",
                },
            };

            _fhirService.RetrievePatientAsync(Arg.Is(TestUtility.BuildIdentifierPredicate(string.Empty, DefaultPatientId)), DefaultCancellationToken)
                .Returns(patient);

            await _patientPipeline.PrepareRequestAsync(context, DefaultCancellationToken);

            FhirTransactionRequestEntry actualPatientEntry = context.Request.Patient;

            ValidationUtility.ValidateRequestEntryMinimumRequirementForNoChange(patient.ToServerResourceId(), actualPatientEntry);
        }

        [Fact]
        public void GivenNoUpdateToExistingPatient_WhenResponseIsProcessed_ThenItShouldBeNoOp()
        {
            // Simulate there is no update to patient resource (and therefore no response).
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            context.Response.Patient = null;

            _patientPipeline.ProcessResponse(context);
        }

        [Fact]
        public void GivenARequestToCreateAPatient_WhenResponseIsOK_ThenResourceConflictExceptionShouldBeThrown()
        {
            var response = new Bundle.ResponseComponent();

            response.AddAnnotation(HttpStatusCode.OK);

            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            context.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Patient>();

            context.Response.Patient = new FhirTransactionResponseEntry(response, new Patient());

            Assert.Throws<ResourceConflictException>(() => _patientPipeline.ProcessResponse(context));
        }

        [Fact]
        public void GivenARequestToUpdateAPatient_WhenResponseIsOK_ThenItShouldBeNoOp()
        {
            var response = new Bundle.ResponseComponent();

            response.AddAnnotation(HttpStatusCode.OK);

            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            context.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultUpdateRequestEntry<Patient>(
                new ServerResourceId(ResourceType.Patient, "123"));

            context.Response.Patient = new FhirTransactionResponseEntry(response, new Patient());

            _patientPipeline.ProcessResponse(context);
        }

        private static FhirTransactionContext CreateFhirTransactionContext()
        {
            return new FhirTransactionContext(ChangeFeedGenerator.Generate(metadata: DefaultDicomDataset));
        }
    }
}
