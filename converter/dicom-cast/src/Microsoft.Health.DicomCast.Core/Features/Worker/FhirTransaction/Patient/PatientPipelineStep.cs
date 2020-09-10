// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Pipeline step for handling <see cref="ImagingStudy"/>.
    /// </summary>
    public class PatientPipelineStep : IFhirTransactionPipelineStep
    {
        private readonly IFhirService _fhirService;
        private readonly IPatientSynchronizer _patientSynchronizer;

        public PatientPipelineStep(
            IFhirService fhirService,
            IPatientSynchronizer patientSynchronizer)
        {
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));
            EnsureArg.IsNotNull(patientSynchronizer, nameof(patientSynchronizer));

            _fhirService = fhirService;
            _patientSynchronizer = patientSynchronizer;
        }

        /// <inheritdoc/>
        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            if (!dataset.TryGetSingleValue(DicomTag.PatientID, out string patientId))
            {
                throw new MissingRequiredDicomTagException(nameof(DicomTag.PatientID));
            }

            var patientIdentifier = new Identifier(string.Empty, patientId);

            FhirTransactionRequestMode requestMode = FhirTransactionRequestMode.None;

            Patient existingPatient = await _fhirService.RetrievePatientAsync(patientIdentifier, cancellationToken);
            Patient patient = (Patient)existingPatient?.DeepCopy();

            if (existingPatient == null)
            {
                patient = new Patient();

                patient.Identifier.Add(patientIdentifier);

                requestMode = FhirTransactionRequestMode.Create;
            }

            _patientSynchronizer.Synchronize(dataset, patient);

            if (requestMode == FhirTransactionRequestMode.None &&
                !existingPatient.IsExactly(patient))
            {
                requestMode = FhirTransactionRequestMode.Update;
            }

            Bundle.RequestComponent request = requestMode switch
            {
                FhirTransactionRequestMode.Create => GenerateCreateRequest(patientIdentifier),
                FhirTransactionRequestMode.Update => GenerateUpdateRequest(patient),
                _ => null
            };

            IResourceId resourceId = requestMode switch
            {
                FhirTransactionRequestMode.Create => new ClientResourceId(),
                _ => existingPatient.ToServerResourceId(),
            };

            context.Request.Patient = new FhirTransactionRequestEntry(
                requestMode,
                request,
                resourceId,
                patient);
        }

        /// <inheritdoc/>
        public void ProcessResponse(FhirTransactionContext context)
        {
            // If the Patient does not exist, we will use conditional create to create the resource
            // to avoid duplicated resource being created. However, if the resource with the identifier
            // was created externally between the retrieve and create, conditional create will return 200
            // and might not contain the changes so we will need to try again.
            if (context.Request.Patient?.RequestMode == FhirTransactionRequestMode.Create)
            {
                FhirTransactionResponseEntry patient = context.Response.Patient;

                HttpStatusCode statusCode = patient.Response.Annotation<HttpStatusCode>();

                if (statusCode == HttpStatusCode.OK)
                {
                    throw new ResourceConflictException();
                }
            }
        }

        private static Bundle.RequestComponent GenerateCreateRequest(Identifier patientIdentifier)
        {
            return new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.POST,
                IfNoneExist = patientIdentifier.ToSearchQueryParameter(),
                Url = ResourceType.Patient.GetLiteral(),
            };
        }

        private static Bundle.RequestComponent GenerateUpdateRequest(Patient patient)
        {
            return new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.PUT,
                IfMatch = new EntityTagHeaderValue($"\"{patient.Meta.VersionId}\"", true).ToString(),
                Url = $"{ResourceType.Patient.GetLiteral()}/{patient.Id}",
            };
        }
    }
}
