// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    internal static class EnvironmentConfig
    {
        public static IConfiguration FromLocalSettings(string filePath)
        {
            using FileStream file = File.OpenRead(Path.Combine(filePath, ScriptConstants.LocalSettingsFileName));
            Settings settings = JsonSerializer.Deserialize<Settings>(file);

            if (settings.Encrypted)
            {
                throw new InvalidOperationException($"Cannot process encrypted settings at '{filePath}'.");
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings.Values.Select(x => KeyValuePair.Create(x.Key.Replace("__", ":"), x.Value)))
                .Build();
        }

        private sealed class Settings
        {
            public bool Encrypted { get; set; }

            public Dictionary<string, string> Values { get; set; }
        }
    }
}
