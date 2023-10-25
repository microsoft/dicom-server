// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Dicom.SchemaManager;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder => builder.AddSchemaCommandLine(args))
            .ConfigureServices((context, collection) => collection.AddSchemaManager(context.Configuration))
            .Build();

        return SchemaManagerParser
            .Build(host.Services)
            .InvokeAsync(args);
    }
}
