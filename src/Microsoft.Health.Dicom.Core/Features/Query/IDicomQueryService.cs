// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public interface IDicomQueryService
    {
        Task<DicomQueryResult> QueryAsync(
            DicomQueryOptions query,
            CancellationToken cancellationToken);
    }
}
