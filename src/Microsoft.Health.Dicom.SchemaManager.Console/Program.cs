// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.SchemaManager;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        ServiceCollection serviceCollection = SchemaManagerServiceCollectionBuilder.Build(args);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        Parser parser = SchemaManagerParser.Build(serviceProvider);

        return await parser.InvokeAsync(args).ConfigureAwait(false);
    }
}
