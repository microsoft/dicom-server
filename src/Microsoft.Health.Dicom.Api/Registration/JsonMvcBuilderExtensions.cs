// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Formatters;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class JsonMvcBuilderExtensions
    {
        public static IMvcBuilder AddJsonSerializerOptions(this IMvcBuilder builder, Action<JsonSerializerOptions> configure)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            EnsureArg.IsNotNull(configure, nameof(configure));

            builder.AddJsonOptions(o => configure(o.JsonSerializerOptions));
            builder.Services.Configure(configure);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<MvcOptions>, DicomJsonOutputFormatterPostConfigureOptions>());

            return builder;
        }

        private sealed class DicomJsonOutputFormatterPostConfigureOptions : IPostConfigureOptions<MvcOptions>
        {
            private readonly JsonOptions _jsonOptions;

            public DicomJsonOutputFormatterPostConfigureOptions(IOptions<JsonOptions> jsonOptions)
                => _jsonOptions = EnsureArg.IsNotNull(jsonOptions?.Value, nameof(jsonOptions));

            public void PostConfigure(string name, MvcOptions options)
                => options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter(_jsonOptions));
        }
    }
}
