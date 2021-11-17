// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IObservationDeleteHandler
    {
        Task<IEnumerable<FhirTransactionRequestEntry>> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken);
    }
}
