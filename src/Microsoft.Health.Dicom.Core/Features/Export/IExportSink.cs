// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public interface IExportSink : IAsyncDisposable
{
    event EventHandler<CopyFailureEventArgs> CopyFailure;

    Uri ErrorHref { get; }

    Task<bool> CopyAsync(VersionedInstanceIdentifier identifier, CancellationToken cancellationToken = default);
}
