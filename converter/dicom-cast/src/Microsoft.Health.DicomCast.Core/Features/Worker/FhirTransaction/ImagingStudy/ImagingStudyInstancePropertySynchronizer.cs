// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudyInstancePropertySynchronizer : IImagingStudyInstancePropertySynchronizer
    {
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly IExceptionStore _exceptionStore;

        public ImagingStudyInstancePropertySynchronizer(
            DicomValidationConfiguration dicomValidationConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _dicomValidationConfiguration = dicomValidationConfiguration;
            _exceptionStore = exceptionStore;
        }

        /// <inheritdoc/>
        public void Synchronize(FhirTransactionContext context, ImagingStudy.InstanceComponent instance, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(instance, nameof(instance));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            SynchronizePropertiesAsync(instance, context, true, AddSopClass, cancellationToken);
            SynchronizePropertiesAsync(instance, context, false, AddInstanceNumber, cancellationToken);
        }

        private void SynchronizePropertiesAsync(ImagingStudy.InstanceComponent instance, FhirTransactionContext context, bool required, Action<ImagingStudy.InstanceComponent, FhirTransactionContext> synchronizeAction, CancellationToken cancellationToken = default)
        {
            try
            {
                synchronizeAction(instance, context);
            }
            catch (Exception ex)
            {
                if (_dicomValidationConfiguration.PartialValidation && !required)
                {
                    DicomDataset dataset = context.ChangeFeedEntry.Metadata;
                    string studyUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                    string seriesUID = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                    string instanceUID = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                    _exceptionStore.StoreException(
                        studyUID,
                        seriesUID,
                        instanceUID,
                        context.ChangeFeedEntry.Sequence,
                        ex,
                        TableErrorType.DicomError,
                        cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }

        private void AddSopClass(ImagingStudy.InstanceComponent instance, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.SOPClassUID, out string sopClassUid) &&
                !string.Equals(instance.SopClass?.Code, sopClassUid, StringComparison.Ordinal))
            {
                instance.SopClass = new Coding(null, sopClassUid);
            }
        }

        private void AddInstanceNumber(ImagingStudy.InstanceComponent instance, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.InstanceNumber, out int instanceNumber))
            {
                instance.Number = instanceNumber;
            }
        }
    }
}
