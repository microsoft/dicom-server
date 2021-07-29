// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Health.Dicom.Core.Serialization;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization
{
    public class JsonGuidConverterTests
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions();

        [Fact]
        public void GivenJsonGuidConverter_WhenCheckingNullHandling_ThenReturnFalse()
        {
            Assert.False(new JsonGuidConverter("N").HandleNull);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("42")]
        [InlineData("{ \"foo\": \"bar\" }")]
        [InlineData("[ 1, 2, 3 ]")]
        [InlineData("\"\"")]
        [InlineData("\"bar\"")]
        [InlineData("\"0123456789abcdef0123456789abcde\"")]
        public void GivenInvalidToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
        {
            var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            Assert.True(jsonReader.Read());
            try
            {
                new JsonGuidConverter("N").Read(ref jsonReader, typeof(Guid), DefaultOptions);
                throw new ThrowsException(typeof(JsonException));
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(JsonException))
                {
                    throw new ThrowsException(typeof(JsonException), e);
                }
            }
        }

        [Theory]
        [InlineData("\"foo\"", "N")]
        [InlineData("\"1152d280510c4ce1b4bd651fd2574295\"", "D")]
        [InlineData("\"(4d970ed1-ca80-45e6-b742-6cf2b2e74108)\"", "B")]
        [InlineData("\"{8e10b9fe-3b03-43ec-b0d5-cdcf3971cc0a}\"", "P")]
        [InlineData("\"255d38e6-2df7-4377-87c5-7b2386349d60\"", "X")]
        public void GivenIncorrectFormatStringToken_WhenReadingExactJson_ThenThrowJsonReaderException(string json, string formatSpecifier)
        {
            var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            Assert.True(jsonReader.Read());
            try
            {
                new JsonGuidConverter(formatSpecifier, exactMatch: true).Read(ref jsonReader, typeof(Guid), DefaultOptions);
                throw new ThrowsException(typeof(JsonException));
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(JsonException))
                {
                    throw new ThrowsException(typeof(JsonException), e);
                }
            }
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
            var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"" + expected.ToString(formatSpecifier) + "\""));

            Assert.True(jsonReader.Read());
            Guid actual = new JsonGuidConverter("N", exactMatch: false).Read(ref jsonReader, typeof(Guid), DefaultOptions);
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
            var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"" + expected.ToString(formatSpecifier) + "\""));

            Assert.True(jsonReader.Read());
            Guid actual = new JsonGuidConverter(formatSpecifier, exactMatch: false).Read(ref jsonReader, typeof(Guid), DefaultOptions);
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
            using var buffer = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(buffer);

            new JsonGuidConverter(formatSpecifier, exactMatch: true).Write(jsonWriter, expected, DefaultOptions);
            jsonWriter.Flush();

            buffer.Seek(0, SeekOrigin.Begin);
            var jsonReader = new Utf8JsonReader(buffer.ToArray());

            Assert.Equal(expected.ToString(formatSpecifier), JsonSerializer.Deserialize<string>(ref jsonReader, DefaultOptions));
        }
    }
}
