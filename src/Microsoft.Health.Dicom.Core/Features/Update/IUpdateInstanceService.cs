// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using System.Threading;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Update;
public interface IUpdateInstanceService
{
    public Task UpdateInstanceBlobAsync(long fileIdentifier, long newFileIdentifier, DicomDataset datasetToUpdate, CancellationToken cancellationToken = default);
}
