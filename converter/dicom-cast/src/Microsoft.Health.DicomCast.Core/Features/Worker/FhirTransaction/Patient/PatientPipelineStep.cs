// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using EnsureThat;
using FellowOakDicom;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

/// <summary>
/// Pipeline step for handling <see cref="ImagingStudy"/>.
/// </summary>
public class PatientPipelineStep : FhirTransactionPipelineStepBase
{
    private readonly IFhirService _fhirService;
    private readonly IPatientSynchronizer _patientSynchronizer;
    private readonly string _patientSystemId;
    private readonly bool _isIssuerIdUsed;
    private readonly ILogger<PatientPipelineStep> _logger;

    public PatientPipelineStep(
        IFhirService fhirService,
        IPatientSynchronizer patientSynchronizer,
        IOptions<PatientConfiguration> patientConfiguration,
        ILogger<PatientPipelineStep> logger)
        : base(logger)
    {
        EnsureArg.IsNotNull(fhirService, nameof(fhirService));
        EnsureArg.IsNotNull(patientSynchronizer, nameof(patientSynchronizer));
        EnsureArg.IsNotNull(patientConfiguration?.Value, nameof(patientConfiguration));

        _fhirService = fhirService;
        _patientSynchronizer = patientSynchronizer;
        _patientSystemId = patientConfiguration.Value.PatientSystemId;
        _isIssuerIdUsed = patientConfiguration.Value.IsIssuerIdUsed;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task PrepareRequestImplementationAsync(FhirTransactionContext context, CancellationToken cancellationToken)
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

        // Patient system id is determined based on issuer id boolean
        // If issuer id boolean is set to true, patient system id would be set to issuer of patient id (0010,0021)
        // Otherwise we will be using the patient system id configured during user provisioning
        string patientSystemId = string.Empty;
        if (_isIssuerIdUsed)
        {
            if (dataset.TryGetSingleValue(DicomTag.IssuerOfPatientID, out string systemId))
            {
                patientSystemId = systemId;
            }
        }
        else
        {
            patientSystemId = _patientSystemId;
        }
        
        var patientIdentifier = new Identifier(patientSystemId, patientId);
        FhirTransactionRequestMode requestMode = FhirTransactionRequestMode.None;

        Patient existingPatient = await _fhirService.RetrievePatientAsync(patientIdentifier, cancellationToken);
        var patient = (Patient)existingPatient?.DeepCopy();

        if (existingPatient == null)
        {
            patient = new Patient();

            patient.Identifier.Add(patientIdentifier);

            requestMode = FhirTransactionRequestMode.Create;
        }

        await _patientSynchronizer.SynchronizeAsync(context, patient, requestMode.Equals(FhirTransactionRequestMode.Create), cancellationToken);

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
    protected override void ProcessResponseImplementation(FhirTransactionContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

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
