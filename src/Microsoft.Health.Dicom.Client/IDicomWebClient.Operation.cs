// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Client;

public partial interface IDicomWebClient
{
    Task<DicomWebResponse<OperationState<DicomOperation>>> GetOperationStateAsync(Guid operationId, CancellationToken cancellationToken = default);
}
