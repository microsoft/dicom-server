// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Functions.Client.Extensions;

internal static class AzureComponentFactoryExtensions
{
    public static TClient CreateClient<TOptions, TClient>(this AzureComponentFactory factory, IConfigurationSection configuration)
    {
        EnsureArg.IsNotNull(factory, nameof(factory));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        TokenCredential credential = configuration.Value is null ? factory.CreateTokenCredential(configuration) : null;
        var options = (TOptions)factory.CreateClientOptions(typeof(TOptions), null, configuration);
        return (TClient)factory.CreateClient(typeof(TClient), configuration, credential, options);
    }
}
