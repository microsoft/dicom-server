// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IVersionedInstanceEphimeralCache
    {
        Task<InstanceMetadata> GetInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            Func<string, string, string, CancellationToken, Task<IEnumerable<VersionedInstanceIdentifier>>> getInstanceFunc,
            Func<VersionedInstanceIdentifier, CancellationToken, Task<DicomDataset>> getInstanceMetadataFunc,
            CancellationToken cancellationToken = default);
    }
}
