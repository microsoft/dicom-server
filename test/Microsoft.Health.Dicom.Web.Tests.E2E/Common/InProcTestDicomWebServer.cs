// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Development.IdentityProvider.Registration;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E
{
    /// <summary>
    /// A <see cref="TestDicomWebServer"/> that runs the Dicom server in-process with IdentityServer
    /// <see cref="TestServer"/>.
    /// </summary>
    public class InProcTestDicomWebServer : TestDicomWebServer
    {
        public InProcTestDicomWebServer(Type startupType, bool enableDataPartitions)
            : base(new Uri("http://localhost/"))
        {
            var contentRoot = GetProjectPath("src", startupType);

            var authSettings = new Dictionary<string, string>
            {
                { "TestAuthEnvironment:FilePath", "testauthenvironment.json" },
                { "DicomServer:Security:Authentication:Authority", "https://inprochost" },
                { "DicomServer:Security:Authorization:Enabled", "true" },
                { "DicomServer:Security:Enabled", "true" },
            };

            var featureSettings = new Dictionary<string, string>
            {
                { "DicomServer:Features:EnableExtendedQueryTags", "true" },
                { "DicomServer:Features:EnableDataPartitions", enableDataPartitions.ToString() },
            };

            var dbName = enableDataPartitions ? "DicomWithPartitions" : "Dicom";

            // Overriding sqlserver connection string to provide different DB for data partition test
            // This will ensure there is no multiple partitions when the flag is off
            var sqlSettings = new Dictionary<string, string>
            {
                { "SqlServer:ConnectionString", $"server=(local);Initial Catalog={dbName};Integrated Security=true" },
            };

            IWebHostBuilder builder = WebHost.CreateDefaultBuilder()
                .UseContentRoot(contentRoot)
                .UseStartup(startupType)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddInMemoryCollection(authSettings);
                    config.AddInMemoryCollection(featureSettings);
                    config.AddInMemoryCollection(sqlSettings);
                    var existingConfig = config.Build();
                    config.AddDevelopmentAuthEnvironmentIfConfigured(existingConfig, "DicomServer");
                    if (string.Equals(existingConfig["DicomServer:Security:Enabled"], bool.TrueString, StringComparison.OrdinalIgnoreCase))
                    {
                        Environment.SetEnvironmentVariable("security_enabled", "true");
                    }
                })
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection
                        .AddHttpClient(Options.DefaultName)
                        .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler())
                        .SetHandlerLifetime(Timeout.InfiniteTimeSpan); // So that it is not disposed after 2 minutes;

                    serviceCollection.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options => options.BackchannelHttpHandler = Server.CreateHandler());
                });

            Server = new TestServer(builder);
        }

        public TestServer Server { get; }

        public override HttpMessageHandler CreateMessageHandler()
        {
            return Server.CreateHandler();
        }

        public override void Dispose()
        {
            Server?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Gets the full path to the target project that we wish to test
        /// </summary>
        /// <param name="projectRelativePath">
        /// The parent directory of the target project.
        /// e.g. src, samples, test, or test/Websites
        /// </param>
        /// <param name="startupType">The startup type</param>
        /// <returns>The full path to the target project.</returns>
        private static string GetProjectPath(string projectRelativePath, Type startupType)
        {
            for (Type type = startupType; type != null; type = type.BaseType)
            {
                // Get name of the target project which we want to test
                var projectName = type.GetTypeInfo().Assembly.GetName().Name;

                // Get currently executing test project path
                var applicationBasePath = AppContext.BaseDirectory;

                // Find the path to the target project
                var directoryInfo = new DirectoryInfo(applicationBasePath);
                do
                {
                    directoryInfo = directoryInfo.Parent;

                    var projectDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, projectRelativePath));
                    if (projectDirectoryInfo.Exists)
                    {
                        var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, projectName, $"{projectName}.csproj"));
                        if (projectFileInfo.Exists)
                        {
                            return Path.Combine(projectDirectoryInfo.FullName, projectName);
                        }
                    }
                }
                while (directoryInfo.Parent != null);
            }

            throw new Exception($"Project root could not be located for startup type {startupType.FullName}");
        }
    }
}
