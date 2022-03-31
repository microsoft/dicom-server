// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Newtonsoft.Json;
using Microsoft.Health.Dicom.Core.Serialization.Newtonsoft;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Dicom.Core.Extensions;

/// <summary>
/// Provides <see langword="static"/> methods configuring <see cref="JsonSerializerSettings"/>.
/// </summary>
public static class JsonSerializerSettingsExtensions
{
    /// <summary>
    /// Configures an instance of <see cref="JsonSerializerSettings"/> for usage within
    /// the DICOM server and related services.
    /// </summary>
    /// <param name="settings">A set of existing options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static void ConfigureDefaultDicomSettings(this JsonSerializerSettings settings)
    {
        EnsureArg.IsNotNull(settings, nameof(settings));

        settings.Converters.Clear();
        settings.Converters.Add(new StringEnumConverter());
        settings.Converters.Add(new ConfigurationJsonConverter());
        settings.Converters.Add(new DicomIdentifierJsonConverter());
        settings.Converters.Add(new SourceManifestJsonConverter());

        settings.DateParseHandling = DateParseHandling.None;
        settings.TypeNameHandling = TypeNameHandling.None;
    }
}
