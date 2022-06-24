// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.SchemaManager;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;
using Microsoft.Health.SqlServer.Registration;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        ServiceProvider serviceProvider = BuildServiceProvider(args);
        Parser parser = BuildParser(serviceProvider);

        return await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    private static Parser BuildParser(ServiceProvider serviceProvider)
    {
        var commandLineBuilder = new CommandLineBuilder();

        foreach (Command command in serviceProvider.GetServices<Command>())
        {
            commandLineBuilder.Command.AddCommand(command);
        }

        return commandLineBuilder.UseDefaults().Build();
    }

    private static ServiceProvider BuildServiceProvider(string[] args)
    {
        var services = new ServiceCollection();

        services.AddCliCommands();

        services.SetCommandLineOptions(args);

        services.AddOptions<SqlServerDataStoreConfiguration>().Configure<IOptions<CommandLineOptions>>((s, c) =>
        {
            s.ConnectionString = c.Value.ConnectionString;
            s.AuthenticationType = c.Value.AuthenticationType ?? SqlServerAuthenticationType.ConnectionString;

            if (!string.IsNullOrWhiteSpace(c.Value.ManagedIdentityClientId))
            {
                s.ManagedIdentityClientId = c.Value.ManagedIdentityClientId;
            }
        });

        services.AddSqlServerConnection();

        services.AddSqlServerManagement<SchemaVersion>();

        services.AddSingleton<IBaseSchemaRunner, BaseSchemaRunner>();

        services.AddMediatR(typeof(SchemaUpgradedNotification).Assembly);

        services.AddSingleton<ISchemaClient, DicomSchemaClient>();
        services.AddSingleton<ISchemaManager, SqlSchemaManager>();
        services.AddLogging(configure => configure.AddConsole());
        return services.BuildServiceProvider();
    }
}
