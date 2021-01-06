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
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.TableStorage;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to <see cref="Patient"/> resource.
    /// </summary>
    public class PatientSynchronizer : IPatientSynchronizer
    {
        private readonly IEnumerable<IPatientPropertySynchronizer> _patientPropertySynchronizers;
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly ITableStoreService _tableStoreService;
        private readonly ILogger<PatientSynchronizer> _logger;

        public PatientSynchronizer(
            IEnumerable<IPatientPropertySynchronizer> patientPropertySynchronizers,
            DicomValidationConfiguration dicomValidationConfiguration,
            ITableStoreService tableStoreService,
            ILogger<PatientSynchronizer> logger)
        {
            EnsureArg.IsNotNull(patientPropertySynchronizers, nameof(patientPropertySynchronizers));
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(tableStoreService, nameof(tableStoreService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _patientPropertySynchronizers = patientPropertySynchronizers;
            _dicomValidationConfiguration = dicomValidationConfiguration;
            _tableStoreService = tableStoreService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async void SynchronizeAsync(DicomDataset dataset, Patient patient, bool isNewPatient, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(patient, nameof(patient));

            foreach (IPatientPropertySynchronizer patientPropertySynchronizer in _patientPropertySynchronizers)
            {
                try
                {
                    patientPropertySynchronizer.Synchronize(dataset, patient, isNewPatient);
                }
                catch (Exception ex)
                {
                    if (_dicomValidationConfiguration.PartialValidation && _tableStoreService is TableStoreService && !patientPropertySynchronizer.IsRequired())
                    {
                        string studyUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                        string seriesUID = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                        string instanceUID = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                        await _tableStoreService.StoreException(
                            studyUID,
                            seriesUID,
                            instanceUID,
                            ex,
                            TableErrorType.DicomError,
                            cancellationToken);
                        _logger.LogInformation(ex, "Error when synchronizing patient data for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID} stored into table storage", studyUID, seriesUID, instanceUID);
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
