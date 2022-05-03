// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal interface IExportService
{
    Task<OperationReference> StartExportAsync(ExportSpecification specification, PartitionEntry partition, CancellationToken cancellationToken = default);
}
