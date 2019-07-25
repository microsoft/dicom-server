// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    public interface IDicomInstanceMetadataStore
    {
        Task AddInstanceMetadataAsync(DicomDataset instanceMetadata, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default);

        Task DeleteInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default);
    }
}
