// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common;

public interface IFileCopyStore
{
    /// <summary>
    /// Async copy file from source to a destination target
    /// </summary>
    /// <param name="instanceIdentifier"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CopyFileAsync(VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken = default);
}
