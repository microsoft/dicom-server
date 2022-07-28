// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.BlobMigration;

/// <summary>
/// Represents a service which copies and deletes DICOM instance.
/// </summary>
public interface IBlobMigrationService
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

    /// <summary>
    /// Asynchronously deletes old DICOM instance with the <paramref name="versionedInstanceIdentifier"/>    
    /// </summary>
    /// <param name="versionedInstanceIdentifier">The versioned instance id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The value of the
    /// <see cref="Task{TResult}.Result"/> indicates whether the the delete was successful.
    /// </returns>
    Task DeleteInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default);
}
