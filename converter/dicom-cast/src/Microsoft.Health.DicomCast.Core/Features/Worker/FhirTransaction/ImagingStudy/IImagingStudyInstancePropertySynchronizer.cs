// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IImagingStudyInstancePropertySynchronizer
    {
        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="instance"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="instance">The instance component within study.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void Synchronize(FhirTransactionContext context, ImagingStudy.InstanceComponent instance, CancellationToken cancellationToken);
    }
}
