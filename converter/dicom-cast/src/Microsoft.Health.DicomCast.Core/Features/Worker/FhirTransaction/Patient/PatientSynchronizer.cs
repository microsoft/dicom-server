// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to <see cref="Patient"/> resource.
    /// </summary>
    public class PatientSynchronizer : IPatientSynchronizer
    {
        private readonly IEnumerable<IPatientPropertySynchronizer> _patientPropertySynchronizers;
        private readonly DicomCastConfiguration _dicomCastconfiguration;
        private readonly IExceptionStore _exceptionStore;

        public PatientSynchronizer(
            IEnumerable<IPatientPropertySynchronizer> patientPropertySynchronizers,
            IOptions<DicomCastConfiguration> dicomCastConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(patientPropertySynchronizers, nameof(patientPropertySynchronizers));
            EnsureArg.IsNotNull(dicomCastConfiguration, nameof(dicomCastConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _patientPropertySynchronizers = patientPropertySynchronizers;
            _dicomCastconfiguration = dicomCastConfiguration.Value;
            _exceptionStore = exceptionStore;
        }

        /// <inheritdoc/>
        public async Task SynchronizeAsync(FhirTransactionContext context, Patient patient, bool isNewPatient, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(patient, nameof(patient));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            foreach (IPatientPropertySynchronizer patientPropertySynchronizer in _patientPropertySynchronizers)
            {
                try
                {
                    patientPropertySynchronizer.Synchronize(dataset, patient, isNewPatient);
                }
                catch (DicomTagException ex)
                {
                    if (!_dicomCastconfiguration.Features.EnforceValidationOfTagValues)
                    {
                        await _exceptionStore.WriteExceptionAsync(
                            context.ChangeFeedEntry,
                            ex,
                            ErrorType.DicomValidationError,
                            cancellationToken);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
