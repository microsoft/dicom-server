// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class ExportSinkFactory : IExportSinkFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ExportDestinationType, IExportSinkProvider> _providers;

    public ExportSinkFactory(IServiceProvider serviceProvider, IEnumerable<IExportSinkProvider> providers)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _providers = EnsureArg.IsNotNull(providers, nameof(providers)).ToDictionary(x => x.Type);
    }

    public IExportSink CreateSink(ExportLocation location)
    {
        if (!_providers.TryGetValue(location.Type, out IExportSinkProvider provider))
            throw new InvalidOperationException();

        return provider.Create(_serviceProvider, location.Configuration);
    }

    public void Validate(ExportLocation location)
    {
        if (!_providers.TryGetValue(location.Type, out IExportSinkProvider provider))
            throw new InvalidOperationException();

        provider.Validate(location.Configuration);
    }
}
