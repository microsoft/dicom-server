// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.Core.Modules
{
    public static class ServiceProviderExtensions
    {
        public static NamedCredentialProvider ResolveNamedCredentialProvider(this IServiceProvider serviceProvider, string name)
        {
            EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            IEnumerable<NamedCredentialProvider> namedCredentialProviders = serviceProvider.GetServices<NamedCredentialProvider>();

            return namedCredentialProviders.First(x => x.Name.Equals(name, StringComparison.Ordinal));
        }
    }
}
