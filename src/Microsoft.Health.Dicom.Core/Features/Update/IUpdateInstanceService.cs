// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Core.Features.Update;
public interface IUpdateInstanceService
{
    public Task UpdateInstanceBlobAsync(InstanceMetadata instanceMetadata, DicomDataset datasetToUpdate, CancellationToken cancellationToken = default);
    public Task QueueUpdateOperationAsync(UpdateSpecification updateSpecification, CancellationToken cancellationToken = default);
}
