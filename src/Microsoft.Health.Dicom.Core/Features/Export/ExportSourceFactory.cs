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

public sealed class ExportSourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ExportSourceType, IExportSourceProvider> _providers;

    public ExportSourceFactory(IServiceProvider serviceProvider, IEnumerable<IExportSourceProvider> providers)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _providers = EnsureArg.IsNotNull(providers, nameof(providers)).ToDictionary(x => x.Type);
    }

    public IExportSource CreateSource(SourceManifest source)
    {
        EnsureArg.IsNotNull(source, nameof(source));

        if (!_providers.TryGetValue(source.Type, out IExportSourceProvider provider))
            throw new InvalidOperationException();

        return provider.Create(_serviceProvider, source.Input);
    }

    public void Validate(SourceManifest source)
    {
        EnsureArg.IsNotNull(source, nameof(source));

        if (!_providers.TryGetValue(source.Type, out IExportSourceProvider provider))
            throw new InvalidOperationException();

        provider.Validate(source.Input);
    }
}
