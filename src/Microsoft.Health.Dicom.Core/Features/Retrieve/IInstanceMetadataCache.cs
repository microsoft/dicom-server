// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public interface IInstanceMetadataCache
{
    public Task<InstanceMetadata> GetAsync(
        object key,
        InstanceIdentifier input,
        bool isOriginalVersionRequested,
        Func<InstanceIdentifier, bool, CancellationToken, Task<InstanceMetadata>> asyncFactory,
        CancellationToken cancellationToken = default);
}
