// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public interface IFramesRangeCache
{
    public Task<IReadOnlyDictionary<int, FrameRange>> GetAsync(
       object key,
       long input,
       Func<long, CancellationToken, Task<IReadOnlyDictionary<int, FrameRange>>> asyncFactory,
       CancellationToken cancellationToken = default);
}
