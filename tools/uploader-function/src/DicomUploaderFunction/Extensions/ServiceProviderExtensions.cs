using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;

namespace DicomUploaderFunction.Extensions;

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