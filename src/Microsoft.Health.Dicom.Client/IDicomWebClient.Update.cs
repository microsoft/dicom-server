// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client;

public partial interface IDicomWebClient
{
    Task<DicomWebResponse<DicomOperationReference>> UpdateStudyAsync(IReadOnlyList<string> studyInstanceUids, DicomDataset dataset, string partitionName = default, CancellationToken cancellationToken = default);
}
