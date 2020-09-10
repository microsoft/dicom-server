// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudyInstancePropertySynchronizer : IImagingStudyInstancePropertySynchronizer
    {
        /// <inheritdoc/>
        public void Synchronize(FhirTransactionContext context, ImagingStudy.InstanceComponent instance)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(instance, nameof(instance));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            // Add sopclass to instance
            if (dataset.TryGetSingleValue(DicomTag.SOPClassUID, out string sopClassUid) &&
                !string.Equals(instance.SopClass?.Code, sopClassUid, StringComparison.Ordinal))
            {
                instance.SopClass = new Coding(null, sopClassUid);
            }

            // Add instancenumber to instance
            if (dataset.TryGetSingleValue(DicomTag.InstanceNumber, out int instanceNumber))
            {
                instance.Number = instanceNumber;
            }
        }
    }
}
