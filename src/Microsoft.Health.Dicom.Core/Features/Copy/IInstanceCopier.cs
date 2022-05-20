// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Copy;

/// <summary>
/// Represents a copier which copy DICOM instance.
/// </summary>
public interface IInstanceCopier
{
    /// <summary>
    /// Asynchronously copy the DICOM instance with the <paramref name="versionedInstanceIdentifier"/>    
    /// </summary>
    /// <param name="versionedInstanceIdentifier">The versioned instance id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The value of the
    /// <see cref="Task{TResult}.Result"/> indicates whether the the copy was successful.
    /// </returns>
    Task CopyInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default);
}
