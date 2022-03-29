// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public interface IExportService
{
    /// <summary>
    /// Add Extended Query Tags.
    /// </summary>
    /// <param name="exportInput">The export input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public Task<ExportResponse> ExportAsync(ExportInput exportInput, CancellationToken cancellationToken = default);
}
