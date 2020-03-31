// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IDicomRetrieveMetadataService
    {
        Task<IEnumerable<DicomDataset>> GetDicomInstanceMetadataAsync(
           ResourceType resourceType,
           string studyInstanceUid,
           string seriesInstanceUid,
           string sopInstanceUid,
           CancellationToken cancellationToken = default);
    }
}
