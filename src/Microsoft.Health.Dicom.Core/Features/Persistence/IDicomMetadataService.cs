// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomMetadataService
    {
        Task AddInstanceMetadataAsync(DicomDataset instanceMetadata, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetInstanceMetadataAsync(DicomInstanceIdentifier instance, CancellationToken cancellationToken = default);

        Task DeleteInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default);
    }
}
