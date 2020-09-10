// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading;
using Hl7.Fhir.Model;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyPipelineStepTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IImagingStudyDeleteHandler _imagingStudyDeleteHandler;
        private readonly IImagingStudyUpsertHandler _imagingStudyUpsertHandler;
        private readonly ImagingStudyPipelineStep _imagingStudyPipeline;

        public ImagingStudyPipelineStepTests()
        {
            _imagingStudyDeleteHandler = Substitute.For<IImagingStudyDeleteHandler>();
            _imagingStudyUpsertHandler = Substitute.For<IImagingStudyUpsertHandler>();

            _imagingStudyPipeline = new ImagingStudyPipelineStep(_imagingStudyUpsertHandler, _imagingStudyDeleteHandler);
        }

        [Fact]
        public async Task GivenAChangeFeedEntryForCreate_WhenPreparingTheRequest_ThenCreateHandlerIsCalled()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";

            ChangeFeedEntry changeFeed = ChangeFeedGenerator.Generate(
                    action: ChangeFeedAction.Create,
                    studyInstanceUid: studyInstanceUid,
                    seriesInstanceUid: seriesInstanceUid,
                    sopInstanceUid: sopInstanceUid);

            var fhirTransactionContext = new FhirTransactionContext(changeFeed);

            await _imagingStudyPipeline.PrepareRequestAsync(fhirTransactionContext, DefaultCancellationToken);

            await _imagingStudyUpsertHandler.Received(1).BuildAsync(fhirTransactionContext, DefaultCancellationToken);
        }

        [Fact]
        public void GivenARequestToCreateAnImagingStudy_WhenResponseIsOK_ThenResourceConflictExceptionShouldBeThrown()
        {
            var response = new Bundle.ResponseComponent();

            response.AddAnnotation(HttpStatusCode.OK);

            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate());

            context.Request.ImagingStudy = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<ImagingStudy>();

            context.Response.ImagingStudy = new FhirTransactionResponseEntry(response, new ImagingStudy());

            Assert.Throws<ResourceConflictException>(() => _imagingStudyPipeline.ProcessResponse(context));
        }

        [Fact]
        public async Task GivenAChangeFeedEntryForDelete_WhenBuilt_ThenDeleteHandlerIsCalled()
        {
            const string studyInstanceUid = "1";
            const string seriesInstanceUid = "2";
            const string sopInstanceUid = "3";
            const string patientResourceId = "p1";

            ChangeFeedEntry changeFeed = ChangeFeedGenerator.Generate(
                    action: ChangeFeedAction.Delete,
                    studyInstanceUid: studyInstanceUid,
                    seriesInstanceUid: seriesInstanceUid,
                    sopInstanceUid: sopInstanceUid);

            var fhirTransactionContext = new FhirTransactionContext(changeFeed);

            fhirTransactionContext.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Patient>(
                new ServerResourceId(ResourceType.Patient, patientResourceId));

            await _imagingStudyPipeline.PrepareRequestAsync(fhirTransactionContext, DefaultCancellationToken);

            await _imagingStudyDeleteHandler.Received(1).BuildAsync(fhirTransactionContext, DefaultCancellationToken);
        }
    }
}
