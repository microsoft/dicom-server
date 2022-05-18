// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal abstract class ExportSinkProvider<TOptions> : IExportSinkProvider where TOptions : class, new()
{
    public abstract ExportDestinationType Type { get; }

    public Task<IExportSink> CreateAsync(IServiceProvider provider, IConfiguration config, Guid operationId, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(provider, nameof(provider));
        EnsureArg.IsNotNull(config, nameof(config));

        var options = new TOptions();
        config.Bind(options, o => o.BindNonPublicProperties = true);
        return CreateAsync(provider, options, operationId, cancellationToken);
    }

    public async Task<IConfiguration> SecureSensitiveInfoAsync(IConfiguration config, Guid operationId, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        var options = new TOptions();
        config.Bind(options);

        TOptions result = await SecureSensitiveInfoAsync(options, operationId, cancellationToken);

        IConfiguration validated = new ConfigurationRoot(new IConfigurationProvider[] { new MemoryConfigurationProvider(new MemoryConfigurationSource()) });
        validated.Set(result, c => c.BindNonPublicProperties = true);
        return validated;
    }

    public Task ValidateAsync(IConfiguration config, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        var options = new TOptions();
        config.Bind(options);
        return ValidateAsync(options, cancellationToken);
    }

    protected abstract Task<IExportSink> CreateAsync(IServiceProvider provider, TOptions options, Guid operationId, CancellationToken cancellationToken = default);

    protected abstract Task<TOptions> SecureSensitiveInfoAsync(TOptions options, Guid operationId, CancellationToken cancellationToken = default);

    protected abstract Task ValidateAsync(TOptions options, CancellationToken cancellationToken = default);
}
