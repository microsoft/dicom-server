// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public interface IUpdateInstanceService
{
    public Task<OperationReference> UpdateInstanceAsync(
        UpdateSpecification spec,
        CancellationToken cancellationToken = default);
}
