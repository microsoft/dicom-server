// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.FellowOakDicom.Serialization;
using Microsoft.Health.Dicom.Core.Serialization;

namespace Microsoft.Health.Dicom.Core.Extensions;

/// <summary>
/// Provides <see langword="static"/> methods configuring <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Configures an instance of <see cref="JsonSerializerOptions"/> for usage within
    /// the DICOM server and related services.
    /// </summary>
    /// <param name="options">A set of existing options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static void ConfigureDefaultDicomSettings(this JsonSerializerOptions options)
    {
        EnsureArg.IsNotNull(options, nameof(options));

        options.Converters.Clear();
        options.Converters.Add(new StrictStringEnumConverterFactory());
        options.Converters.Add(new DataSourceJsonConverterFactory());
        options.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: false, autoValidate: false));

        options.AllowTrailingCommas = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.Encoder = null;
        options.IgnoreReadOnlyFields = false;
        options.IgnoreReadOnlyProperties = false;
        options.IncludeFields = false;
        options.MaxDepth = 0; // 0 indicates the max depth of 64
        options.NumberHandling = JsonNumberHandling.Strict;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.WriteIndented = false;
    }
}
