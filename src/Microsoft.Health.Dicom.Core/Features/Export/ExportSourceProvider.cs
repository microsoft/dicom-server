// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal abstract class ExportSourceProvider<TOptions> : IExportSourceProvider where TOptions : class, new()
{
    public abstract ExportSourceType Type { get; }

    public Task<IExportSource> CreateAsync(IServiceProvider provider, IConfiguration config, PartitionEntry partition, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(provider, nameof(provider));
        EnsureArg.IsNotNull(config, nameof(config));
        EnsureArg.IsNotNull(partition, nameof(partition));

        var options = new TOptions();
        config.Bind(options);
        return CreateAsync(provider, options, partition, cancellationToken);
    }

    public Task ValidateAsync(IConfiguration config, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        var options = new TOptions();
        config.Bind(options);
        return ValidateAsync(options, cancellationToken);
    }

    protected abstract Task<IExportSource> CreateAsync(IServiceProvider provider, TOptions options, PartitionEntry partition, CancellationToken cancellationToken = default);

    protected abstract Task ValidateAsync(TOptions options, CancellationToken cancellationToken = default);
}
