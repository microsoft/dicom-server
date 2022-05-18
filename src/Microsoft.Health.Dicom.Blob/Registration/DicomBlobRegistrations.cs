// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Blob;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Export;

namespace Microsoft.Extensions.DependencyInjection;

internal static class DicomBlobRegistrations
{
    public static IServiceCollection AddAzureBlobExportSink(this IServiceCollection services, Action<AzureBlobClientOptions> configure = null)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IExportSinkProvider, AzureBlobExportSinkProvider>());

        OptionsBuilder<AzureBlobClientOptions> builder = services.AddOptions<AzureBlobClientOptions>("Export");
        if (configure != null)
            builder.Configure(configure);

        return services;
    }
}
