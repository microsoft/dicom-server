// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public interface IExportSink : IAsyncDisposable
{
    event EventHandler<CopyFailureEventArgs> CopyFailure;

    Task<Uri> GetErrorHrefAsync(CancellationToken cancellationToken = default);

    Task<bool> CopyAsync(SourceElement element, CancellationToken cancellationToken = default);
}
