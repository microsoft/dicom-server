// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
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
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly IExceptionStore _exceptionStore;
        private readonly ILogger<PatientSynchronizer> _logger;

        public PatientSynchronizer(
            IEnumerable<IPatientPropertySynchronizer> patientPropertySynchronizers,
            IOptions<DicomValidationConfiguration> dicomValidationConfiguration,
            IExceptionStore exceptionStore,
            ILogger<PatientSynchronizer> logger)
        {
            EnsureArg.IsNotNull(patientPropertySynchronizers, nameof(patientPropertySynchronizers));
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _patientPropertySynchronizers = patientPropertySynchronizers;
            _dicomValidationConfiguration = dicomValidationConfiguration.Value;
            _exceptionStore = exceptionStore;
            _logger = logger;
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
                catch (Exception ex)
                {
                    if (_dicomValidationConfiguration.PartialValidation && !patientPropertySynchronizer.IsRequired())
                    {
                        string studyUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                        string seriesUID = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                        string instanceUID = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

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

        private static bool IsPatientUpdated(Patient patient, IDeepCopyable patientBeforeSynchronize)
        {
            // check if the patient property has changed after synchronization
            return !patient.IsExactly((IDeepComparable)patientBeforeSynchronize);
        }
    }
}
