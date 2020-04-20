// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public interface IDicomMetadataStore
    {
        Task AddInstanceMetadataAsync(DicomDataset dicomDataset, CancellationToken cancellationToken = default);

        Task<DicomDataset> GetInstanceMetadataAsync(DicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default);

        Task DeleteInstanceMetadataIfExistsAsync(DicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default);
    }
}
