// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomInstanceMetadataStore
    {
        Task AddInstanceMetadataAsync(DicomDataset instanceMetadata, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetInstanceMetadataAsync(DicomInstance instance, HashSet<DicomAttributeId> optionalAttributes, CancellationToken cancellationToken = default);

        Task DeleteInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default);
    }
}
