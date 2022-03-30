// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using EnsureThat;
// using Microsoft.Health.Dicom.Core.Models.Export;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Functions.Serialization;

// internal sealed class DataSourceJsonConverter : JsonConverter<DataSource>
// {
//     public override DataSource ReadJson(JsonReader reader, Type objectType, DataSource existingValue, bool hasExistingValue, JsonSerializer serializer)
//     {
//         DataSource source = serializer.Deserialize<DataSource>(reader);
// 
//         var token = source.Metadata as JToken;
//         if (token == null)
//             throw new JsonException();
// 
//         source.Metadata = source.Type switch
//         {
//             ExportSourceType.UID => token.Value<string[]>(),
//             _ => throw new JsonException()
//         };
//     }
// 
//     public override void WriteJson(JsonWriter writer, DataSource value, JsonSerializer serializer)
//         => serializer.Serialize(writer, value);
// }
