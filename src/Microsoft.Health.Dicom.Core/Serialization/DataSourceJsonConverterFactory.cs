// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Serialization;
public sealed class DataSourceJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the JSON converter can operate on the given type.
    /// </summary>
    /// <param name="typeToConvert">The type to serialize and/or deserialize.</param>
    /// <returns>
    /// <see langword="true"/> if the type is compatible with the converter; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeToConvert"/> is <see langword="null"/>.</exception>
    public override bool CanConvert(Type typeToConvert)
        => EnsureArg.IsNotNull(typeToConvert) == typeof(DataSource);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => Activator.CreateInstance<DataSourceJsonConverter>();
}
