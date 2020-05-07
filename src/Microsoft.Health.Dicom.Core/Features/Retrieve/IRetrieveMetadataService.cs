// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IRetrieveMetadataService
    {
        Task<RetrieveMetadataResponse> RetrieveStudyInstanceMetadataAsync(
           string studyInstanceUid,
           CancellationToken cancellationToken = default);

        Task<RetrieveMetadataResponse> RetrieveSeriesInstanceMetadataAsync(
           string studyInstanceUid,
           string seriesInstanceUid,
           CancellationToken cancellationToken = default);

        Task<RetrieveMetadataResponse> RetrieveSopInstanceMetadataAsync(
           string studyInstanceUid,
           string seriesInstanceUid,
           string sopInstanceUid,
           CancellationToken cancellationToken = default);
    }
}
