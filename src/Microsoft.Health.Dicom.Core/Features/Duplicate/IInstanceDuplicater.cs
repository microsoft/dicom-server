// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Duplicate;
public interface IInstanceDuplicater
{
    /// <summary>
    /// Asynchronously duplicate the DICOM instance with the <paramref name="versionedInstanceId"/>    
    /// </summary>
    /// <param name="versionedInstanceId">The versioned instance id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The value of the
    /// <see cref="Task{TResult}.Result"/> indicates whether the reindexing was successful.
    /// </returns>
    Task DuplicateInstanceAsync(VersionedInstanceIdentifier versionedInstanceId, CancellationToken cancellationToken = default);
}
