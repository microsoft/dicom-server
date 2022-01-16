// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using FellowOakDicom.Serialization;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class JsonSerializerOptionsExtensionsTests
    {
        [Fact]
        public void GivenOptions_WhenConfiguringDefaults_ThenUpdateProperties()
        {
            var actual = new JsonSerializerOptions();
            actual.ConfigureDefaultDicomSettings();

            Assert.Equal(2, actual.Converters.Count);
            Assert.Equal(typeof(StrictStringEnumConverterFactory), actual.Converters[0].GetType());
            Assert.Equal(typeof(DicomJsonConverter), actual.Converters[1].GetType());

            Assert.False(actual.AllowTrailingCommas);
            Assert.Equal(JsonIgnoreCondition.WhenWritingNull, actual.DefaultIgnoreCondition);
            Assert.Equal(JsonNamingPolicy.CamelCase, actual.DictionaryKeyPolicy);
            Assert.Null(actual.Encoder);
            Assert.False(actual.IgnoreReadOnlyFields);
            Assert.False(actual.IgnoreReadOnlyProperties);
            Assert.False(actual.IncludeFields);
            Assert.Equal(0, actual.MaxDepth);
            Assert.Equal(JsonNumberHandling.Strict, actual.NumberHandling);
            Assert.True(actual.PropertyNameCaseInsensitive);
            Assert.Equal(JsonNamingPolicy.CamelCase, actual.PropertyNamingPolicy);
            Assert.Equal(JsonCommentHandling.Disallow, actual.ReadCommentHandling);
            Assert.False(actual.WriteIndented);
        }
    }
}
