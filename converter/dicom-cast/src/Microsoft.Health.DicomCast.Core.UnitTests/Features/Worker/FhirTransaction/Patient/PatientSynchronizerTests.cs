// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Dicom;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class PatientSynchronizerTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private static readonly DicomDataset DefaultDicomDataset = new DicomDataset();

        private IPatientPropertySynchronizer _propertySynchronizer = Substitute.For<IPatientPropertySynchronizer>();

        private readonly DicomCastConfiguration _dicomCastConfig = new DicomCastConfiguration();

        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        [Fact]
        public async Task WhenPatialValidationDisabled_AndError_ThrowsError()
        {
            _dicomCastConfig.Features.IgnoreSyncOfInvalidTagValue = false;

            _propertySynchronizer.When(synchronizer => synchronizer.Synchronize(Arg.Any<DicomDataset>(), Arg.Any<Patient>(), isNewPatient: false)).Do(synchronizer => { throw new InvalidDicomTagValueException("invalid tag", "invalid tag"); });

            IEnumerable<IPatientPropertySynchronizer> patientPropertySynchronizers = new List<IPatientPropertySynchronizer>()
            {
                _propertySynchronizer,
            };

            PatientSynchronizer patientSynchronizer = new PatientSynchronizer(patientPropertySynchronizers, Options.Create(_dicomCastConfig), _exceptionStore);

            FhirTransactionContext context = new FhirTransactionContext(ChangeFeedGenerator.Generate(metadata: DefaultDicomDataset));
            var patient = new Patient();

            await Assert.ThrowsAsync<InvalidDicomTagValueException>(() => patientSynchronizer.SynchronizeAsync(context, patient, false, DefaultCancellationToken));
        }

        [Fact]
        public async Task WhenPatialValidationEnabled_AndPropertyNotRequired_DoesNotThrowError()
        {
            _dicomCastConfig.Features.IgnoreSyncOfInvalidTagValue = true;

            _propertySynchronizer.When(synchronizer => synchronizer.Synchronize(Arg.Any<DicomDataset>(), Arg.Any<Patient>(), isNewPatient: false)).Do(synchronizer => { throw new InvalidDicomTagValueException("invalid tag", "invalid tag"); });

            IEnumerable<IPatientPropertySynchronizer> patientPropertySynchronizers = new List<IPatientPropertySynchronizer>()
            {
                _propertySynchronizer,
            };

            PatientSynchronizer patientSynchronizer = new PatientSynchronizer(patientPropertySynchronizers, Options.Create(_dicomCastConfig), _exceptionStore);

            FhirTransactionContext context = new FhirTransactionContext(ChangeFeedGenerator.Generate(metadata: DefaultDicomDataset));
            var patient = new Patient();

            await patientSynchronizer.SynchronizeAsync(context, patient, false, DefaultCancellationToken);
        }
    }
}
