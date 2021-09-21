// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Task<DicomWebResponse<OperationReference>> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> tagEntries, CancellationToken cancellationToken = default);

        Task<DicomWebResponse> DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<GetExtendedQueryTagEntry>> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IEnumerable<GetExtendedQueryTagEntry>>> GetExtendedQueryTagsAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);

        Task<DicomWebResponse<IEnumerable<ExtendedQueryTagError>>> GetExtendedQueryTagErrorsAsync(string tagPath, int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
    }
}
