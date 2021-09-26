// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Health.Dicom.Api.Features.Converter;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Converter
{
    public class CustomJsonStringEnumConverterTests
    {
        private readonly JsonSerializerOptions _options;
        public CustomJsonStringEnumConverterTests()
        {
            _options = new JsonSerializerOptions();
            _options.Converters.Add(new CustomJsonStringEnumConverter());
        }

        [Theory]
        [InlineData("\"Instance\"", QueryTagLevel.Instance)]
        [InlineData("\"instance\"", QueryTagLevel.Instance)] // lower case
        [InlineData("\"0\"", QueryTagLevel.Instance)] // string number -- Enum.TryParse support this by default.
        public void GivenValidEnumString_WhenRead_ThenShouldReturnExpectedValue(string value, QueryTagLevel expected)
        {
            Assert.Equal(expected, JsonSerializer.Deserialize(value, typeof(QueryTagLevel), _options));
        }

        [Theory]
        [InlineData("\"Enab\"")] // invalid string value
        [InlineData("1")]   // number
        public void GivenInvalidEnumString_WhenRead_ThenShouldReturnExpectedValue(string value)
        {
            var exp = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(value, typeof(QueryTagLevel), _options));
            Assert.Equal("The value is not valid. It need to be one of \"Instance\",\"Series\",\"Study\".", exp.Message);
        }

        [Fact]
        public void GivenNumber_WhenRead_ThenShouldReturnExpectedValue()
        {
            var exp = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("1", typeof(QueryTagLevel), _options));
            Assert.Equal("The value is not valid. It need to be one of \"Instance\",\"Series\",\"Study\".", exp.Message);
        }

        [Fact]
        public void GivenValidEnum_WhenWrite_ThenShouldWriteExpectedValue()
        {
            var actual = JsonSerializer.Serialize(QueryTagLevel.Instance, typeof(QueryTagLevel), _options);
            Assert.Equal("\"Instance\"", actual);
        }
    }
}
