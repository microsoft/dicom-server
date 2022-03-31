// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public sealed class ExportSinkFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ExportDestinationType, IExportSinkProvider> _providers;

    public ExportSinkFactory(IServiceProvider serviceProvider, IEnumerable<IExportSinkProvider> providers)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _providers = EnsureArg.IsNotNull(providers, nameof(providers)).ToDictionary(x => x.Type);
    }

    public IExportSink CreateSink(ExportDestination location)
    {
        EnsureArg.IsNotNull(location, nameof(location));

        if (!_providers.TryGetValue(location.Type, out IExportSinkProvider provider))
            throw new InvalidOperationException();

        return provider.Create(_serviceProvider, GetConfiguration(location.Configuration));
    }

    public void Validate(ExportDestination location)
    {
        EnsureArg.IsNotNull(location, nameof(location));

        if (!_providers.TryGetValue(location.Type, out IExportSinkProvider provider))
            throw new InvalidOperationException();

        provider.Validate(GetConfiguration(location.Configuration));
    }

    private static IConfiguration GetConfiguration(IEnumerable<KeyValuePair<string, string>> pairs)
        => new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
}
