// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal abstract class ExportSinkProvider<TOptions> : IExportSinkProvider
{
    public abstract ExportDestinationType Type { get; }

    public Task CompleteCopyAsync(object options, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        return CompleteCopyAsync((TOptions)options, cancellationToken);
    }

    public Task<IExportSink> CreateAsync(object options, Guid operationId, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        return CreateAsync((TOptions)options, operationId, cancellationToken);
    }

    public async Task<object> SecureSensitiveInfoAsync(object options, Guid operationId, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        return await SecureSensitiveInfoAsync((TOptions)options, operationId, cancellationToken);
    }

    public Task ValidateAsync(object options, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        return ValidateAsync((TOptions)options, cancellationToken);
    }

    protected abstract Task CompleteCopyAsync(TOptions options, CancellationToken cancellationToken = default);

    protected abstract Task<IExportSink> CreateAsync(TOptions options, Guid operationId, CancellationToken cancellationToken = default);

    protected abstract Task<TOptions> SecureSensitiveInfoAsync(TOptions options, Guid operationId, CancellationToken cancellationToken = default);

    protected abstract Task ValidateAsync(TOptions options, CancellationToken cancellationToken = default);
}
