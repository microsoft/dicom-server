// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Microsoft.Health.Dicom.Core.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization
{
    public class JsonGuidConverterTests
    {
        [Fact]
        public void GivenJsonGuidConverter_WhenCheckingRead_ThenReturnTrue()
        {
            Assert.True(new JsonGuidConverter("N").CanRead);
        }

        [Fact]
        public void GivenJsonGuidConverter_WhenCheckingWrite_ThenReturnTrue()
        {
            Assert.True(new JsonGuidConverter("P").CanWrite);
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("42")]
        [InlineData("{ \"foo\": \"bar\" }")]
        [InlineData("[ 1, 2, 3 ]")]
        public void GivenInvalidToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.Equal(json != "", jsonReader.Read());
            Assert.Throws<JsonReaderException>(
                () => new JsonGuidConverter("N").ReadJson(
                    jsonReader,
                    typeof(Guid),
                    null,
                    new JsonSerializer()));
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"bar\"")]
        [InlineData("\"0123456789abcdef0123456789abcde\"")]
        public void GivenInvalidStringToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            Assert.Throws<JsonReaderException>(
                () => new JsonGuidConverter(formatSpecifier: "D", exactMatch: false).ReadJson(
                    jsonReader,
                    typeof(Guid),
                    null,
                    new JsonSerializer()));
        }

        [Theory]
        [InlineData("\"foo\"", "N")]
        [InlineData("\"1152d280510c4ce1b4bd651fd2574295\"", "D")]
        [InlineData("\"(4d970ed1-ca80-45e6-b742-6cf2b2e74108)\"", "B")]
        [InlineData("\"{8e10b9fe-3b03-43ec-b0d5-cdcf3971cc0a}\"", "P")]
        [InlineData("\"255d38e6-2df7-4377-87c5-7b2386349d60\"", "X")]
        public void GivenIncorrectFormatStringToken_WhenReadingExactJson_ThenThrowJsonReaderException(string json, string formatSpecifier)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            Assert.Throws<JsonReaderException>(
                () => new JsonGuidConverter(formatSpecifier, exactMatch: true).ReadJson(
                    jsonReader,
                    typeof(Guid),
                    null,
                    new JsonSerializer()));
        }

        [Theory]
        [InlineData("N")]
        [InlineData("D")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void GivenStringToken_WhenReadingNonExactJson_ThenReturnGuid(string formatSpecifier)
        {
            Guid expected = Guid.NewGuid();
            using var reader = new StringReader("\"" + expected.ToString(formatSpecifier) + "\"");
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            Guid actual = (Guid)new JsonGuidConverter("N", exactMatch: false).ReadJson(
                    jsonReader,
                    typeof(Guid),
                    null,
                    new JsonSerializer());

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("N")]
        [InlineData("D")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void GivenStringToken_WhenReadingExactJson_ThenReturnGuid(string formatSpecifier)
        {
            Guid expected = Guid.NewGuid();
            using var reader = new StringReader("\"" + expected.ToString(formatSpecifier) + "\"");
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            Guid actual = (Guid)new JsonGuidConverter(formatSpecifier, exactMatch: true).ReadJson(
                    jsonReader,
                    typeof(Guid),
                    null,
                    new JsonSerializer());

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("N")]
        [InlineData("D")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void GivenGuid_WhenWritingJson_ThenThrowNotSupportedException(string formatSpecifier)
        {
            Guid expected = Guid.NewGuid();
            var buffer = new StringBuilder();

            using (var writer = new StringWriter(buffer))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                new JsonGuidConverter(formatSpecifier, exactMatch: true).WriteJson(
                    jsonWriter,
                    expected,
                    new JsonSerializer());
            }

            using var reader = new StringReader(buffer.ToString());
            using var jsonReader = new JsonTextReader(reader);
            Assert.Equal(expected.ToString(formatSpecifier), jsonReader.ReadAsString());
        }
    }
}
