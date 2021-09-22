// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------



using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Web
{
    internal class CustomJsonStringEnumConverter : JsonConverterFactory
    {
        public CustomJsonStringEnumConverter() { }

        public override bool CanConvert(Type typeToConvert)
        {
            EnsureArg.IsNotNull(typeToConvert, nameof(typeToConvert));
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureArg.IsNotNull(typeToConvert, nameof(typeToConvert));
            var baseType = typeof(CustomJsonStringEnumConverterImp<>);
            var genericType = baseType.MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(genericType);
        }
    }
}
