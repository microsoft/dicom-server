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
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudyInstancePropertySynchronizer : IImagingStudyInstancePropertySynchronizer
    {
        private readonly DicomCastConfiguration _dicomCastConfiguration;
        private readonly IExceptionStore _exceptionStore;
        private readonly IEnumerable<(Action<ImagingStudy.InstanceComponent, FhirTransactionContext> PropertyAction, bool RequiredProperty)> _propertiesToSync = new List<(Action<ImagingStudy.InstanceComponent, FhirTransactionContext> PropertyAction, bool RequiredProperty)>()
            {
                (AddSopClass, true),
                (AddInstanceNumber, false),
            };

        public ImagingStudyInstancePropertySynchronizer(
            IOptions<DicomCastConfiguration> dicomCastConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(dicomCastConfiguration, nameof(dicomCastConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _dicomCastConfiguration = dicomCastConfiguration.Value;
            _exceptionStore = exceptionStore;
        }

        /// <inheritdoc/>
        public async Task SynchronizeAsync(FhirTransactionContext context, ImagingStudy.InstanceComponent instance, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(instance, nameof(instance));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            foreach (var property in _propertiesToSync)
            {
                await ImagingStudyPipelineHelper.SynchronizePropertiesAsync(instance, context, property.PropertyAction, property.RequiredProperty, _dicomCastConfiguration.Features.EnforceValidationOfTagValues, _exceptionStore, cancellationToken);
            }
        }

        private static void AddSopClass(ImagingStudy.InstanceComponent instance, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.SOPClassUID, out string sopClassUid) &&
                !string.Equals(instance.SopClass?.Code, sopClassUid, StringComparison.Ordinal))
            {
                instance.SopClass = new Coding(null, sopClassUid);
            }
        }

        private static void AddInstanceNumber(ImagingStudy.InstanceComponent instance, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.InstanceNumber, out int instanceNumber))
            {
                instance.Number = instanceNumber;
            }
        }
    }
}
