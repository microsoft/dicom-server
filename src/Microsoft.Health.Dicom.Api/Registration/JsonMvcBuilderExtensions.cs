// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using EnsureThat;

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
            return builder;
        }
    }
}
