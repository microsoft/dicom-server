// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Registration;
using SchemaManager.Core;

namespace SqlSchemaRunner;

public static class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        var configuration = builder.Build();

        // Add SqlServer services
        services.AddOptions();
        services.AddHttpClient();

        services.AddSqlServerConnection(c => configuration.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(c));
        services.AddSqlServerManagement<SchemaVersion>();
        // TODO: this won't work in OSS if the AuthenticationType is set to ManagedIdentity
        services.AddSingleton<ISqlConnectionBuilder, DefaultSqlConnectionBuilder>();
        services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();
        services.AddSingleton<IBaseSchemaRunner, BaseSchemaRunner>();
        services.AddSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>();
        services.AddSingleton<ISchemaClient, LibrarySchemaClient>();
        services.AddSingleton<ISchemaManager, SqlSchemaManager>();
        services.AddSingleton(SqlConfigurableRetryFactory.CreateNoneRetryProvider());

        services.AddSingleton<ISchemaClient, LibrarySchemaClient>();
        services.AddLogging(configure => configure.AddConsole());

        services.Add(provider => new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max))
            .Singleton()
            .AsSelf();

        var serviceProvider = services.BuildServiceProvider();

        var schemaManager = serviceProvider.GetService<ISchemaManager>();
        var logger = serviceProvider.GetService<ILogger>();
        var connectionStringProvider = serviceProvider.GetService<ISqlConnectionStringProvider>();
        var schemaManagerDataStore = serviceProvider.GetService<ISchemaManagerDataStore>();
        var schemaInformation = serviceProvider.GetService<SchemaInformation>();

        if (schemaManagerDataStore != null)
        {
            int version = schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).Result;
            if (version != 0)
            {
                if (schemaInformation != null)
                {
                    schemaInformation.Current = version;
                }
            }
        }

        var server = new Uri("https://localhost");
        var output = schemaManager?.GetAvailableSchema(server);

        var connectionstring = connectionStringProvider?.GetSqlConnectionString(default).Result;

        var currentVersions = schemaManager?.GetCurrentSchema(connectionstring, server, default).Result;

        if (currentVersions != null)
        {
            var currentVersion = currentVersions.FirstOrDefault();
            Console.WriteLine();
        }

        Console.ReadLine();
    }
}
