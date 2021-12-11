// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EnsureThat;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    // This class is a subset of the same class found inside of the Azure Functions Host:
    // https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/Config/HostJsonFileConfigurationSource.cs
    public class HostJsonFileConfigurationSource : IConfigurationSource
    {
        private readonly ILogger _logger;

        public string ScriptPath { get; }

        public HostJsonFileConfigurationSource(string scriptPath, ILoggerFactory loggerFactory)
        {
            ScriptPath = EnsureArg.IsNotEmpty(scriptPath, nameof(scriptPath));
            _logger = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory)).CreateLogger(LogCategories.Startup);
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new HostJsonFileConfigurationProvider(this, _logger);
        }

        public class HostJsonFileConfigurationProvider : ConfigurationProvider
        {
            private static readonly string[] WellKnownHostJsonProperties = new[]
            {
                "version", "functionTimeout", "retry", "functions", "http", "watchDirectories", "watchFiles", "queues", "serviceBus",
                "eventHub", "singleton", "logging", "aggregator", "healthMonitor", "extensionBundle", "managedDependencies",
                "customHandler", "httpWorker", "extensions", "concurrency"
            };

            private readonly HostJsonFileConfigurationSource _configurationSource;
            private readonly Stack<string> _path;
            private readonly ILogger _logger;

            public HostJsonFileConfigurationProvider(HostJsonFileConfigurationSource configurationSource, ILogger logger)
            {
                _configurationSource = configurationSource;
                _path = new Stack<string>();
                _logger = logger;
            }

            public override void Load()
            {
                JObject hostJson = LoadHostConfigurationFile();
                ProcessObject(hostJson);
            }

            private void ProcessObject(JObject hostJson)
            {
                foreach (var property in hostJson.Properties())
                {
                    _path.Push(property.Name);
                    ProcessProperty(property);
                    _path.Pop();
                }
            }

            private void ProcessProperty(JProperty property)
            {
                ProcessToken(property.Value);
            }

            private void ProcessToken(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        ProcessObject(token.Value<JObject>());
                        break;
                    case JTokenType.Array:
                        ProcessArray(token.Value<JArray>());
                        break;

                    case JTokenType.Integer:
                    case JTokenType.Float:
                    case JTokenType.String:
                    case JTokenType.Boolean:
                    case JTokenType.Null:
                    case JTokenType.Date:
                    case JTokenType.Raw:
                    case JTokenType.Bytes:
                    case JTokenType.TimeSpan:
                        string key = AzureFunctionsConfiguration.JobHostSectionName + ConfigurationPath.KeyDelimiter + ConfigurationPath.Combine(_path.Reverse());
                        Data[key] = token.Value<JValue>().ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        break;
                }
            }

            private void ProcessArray(JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    _path.Push(i.ToString());
                    ProcessToken(jArray[i]);
                    _path.Pop();
                }
            }

            /// <summary>
            /// Read and apply host.json configuration.
            /// </summary>
            private JObject LoadHostConfigurationFile()
            {
                // Before configuration has been fully read, configure a default logger factory
                // to ensure we can log any configuration errors. There's no filters at this point,
                // but that's okay since we can't build filters until we apply configuration below.
                // We'll recreate the loggers after config is read. We initialize the public logger
                // to the startup logger until we've read configuration settings and can create the real logger.
                // The "startup" logger is used in this class for startup related logs. The public logger is used
                // for all other logging after startup.

                string hostFilePath = Path.Combine(_configurationSource.ScriptPath, ScriptConstants.HostMetadataFileName);
                JObject hostConfigObject = LoadHostConfig(hostFilePath);
                hostConfigObject = InitializeHostConfig(hostFilePath, hostConfigObject);
                string sanitizedJson = SanitizeHostJson(hostConfigObject);

                _logger.LogInformation("Loaded configuration from '{Path}'.", hostFilePath);
                _logger.LogDebug("Host configuration:" + Environment.NewLine + "{Config}", sanitizedJson);

                return hostConfigObject;
            }

            private JObject InitializeHostConfig(string hostJsonPath, JObject hostConfigObject)
            {
                // If the object is empty, initialize it to include the version and write the file.
                if (!hostConfigObject.HasValues)
                {
                    _logger.LogWarning("Host configuration is empty");

                    hostConfigObject = GetDefaultHostConfigObject();
                    TryWriteHostJson(hostJsonPath, hostConfigObject);
                }

                string hostJsonVersion = hostConfigObject["version"]?.Value<string>();
                if (string.IsNullOrEmpty(hostJsonVersion))
                {
                    throw new FormatException($"The {ScriptConstants.HostMetadataFileName} file is missing the required 'version' property. See https://aka.ms/functions-hostjson for steps to migrate the configuration file.");
                }

                if (!hostJsonVersion.Equals("2.0"))
                {
                    StringBuilder errorMsg = new StringBuilder($"'{hostJsonVersion}' is an invalid value for {ScriptConstants.HostMetadataFileName} 'version' property. We recommend you set the 'version' property to '2.0'. ");
                    if (hostJsonVersion.StartsWith("3", StringComparison.OrdinalIgnoreCase))
                    {
                        // In case the customer has confused host.json version with the fact that they're running Functions v3
                        errorMsg.Append($"This does not correspond to the function runtime version, only to the schema version of the {ScriptConstants.HostMetadataFileName} file. ");
                    }
                    errorMsg.Append($"See https://aka.ms/functions-hostjson for more information on this configuration file.");
                    throw new FormatException(errorMsg.ToString());
                }

                return hostConfigObject;
            }

            internal JObject LoadHostConfig(string configFilePath)
            {
                JObject hostConfigObject;
                try
                {
                    string json = File.ReadAllText(configFilePath);
                    hostConfigObject = JObject.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new FormatException($"Unable to parse host configuration file '{configFilePath}'.", ex);
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    // if no file exists we default the config
                    _logger.LogError("Cannot find host configuration at '{Path}'.", configFilePath);

                    // There isn't a clean way to create a new function app resource with host.json as initial payload.
                    // So a newly created function app from the portal would have no host.json. In that case we need to
                    // create a new function app with host.json that includes a matching extension bundle based on the app kind.
                    hostConfigObject = GetDefaultHostConfigObject();
                    hostConfigObject = TryAddBundleConfiguration(
                        hostConfigObject,
                        ScriptConstants.DefaultExtensionBundleId,
                        ScriptConstants.DefaultExtensionBundleVersion);

                    // Add bundle configuration if no file exists and file system is not read only
                    TryWriteHostJson(configFilePath, hostConfigObject);
                }

                return hostConfigObject;
            }

            private JObject GetDefaultHostConfigObject()
                => JObject.Parse("{'version': '2.0'}");

            private void TryWriteHostJson(string filePath, JObject content)
            {
                try
                {
                    File.WriteAllText(filePath, content.ToString(Formatting.Indented));
                }
                catch
                {
                    _logger.LogError("Unable to write host configuration to '{Path}'.", filePath);
                }
            }

            private JObject TryAddBundleConfiguration(JObject content, string bundleId, string bundleVersion)
            {
                string bundleConfiguration = "{ 'id': '" + bundleId + "', 'version': '" + bundleVersion + "'}";
                content.Add("extensionBundle", JToken.Parse(bundleConfiguration));
                _logger.LogInformation("Added bundle '{BundleId}' with version '{BundleVersion}'.", bundleId, bundleVersion);

                return content;
            }

            internal static string SanitizeHostJson(JObject hostJsonObject)
            {
                JObject sanitizedObject = new JObject();

                foreach (var propName in WellKnownHostJsonProperties)
                {
                    var propValue = hostJsonObject[propName];
                    if (propValue != null)
                    {
                        sanitizedObject[propName] = propValue;
                    }
                }

                return sanitizedObject.ToString();
            }
        }
    }
}
