// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Functions.Client.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.Functions.Client.UnitTests.Serialization
{
    public class OperationTypeConverterTests
    {
        [Fact]
        public void CanRead()
        {
            Assert.True(new OperationTypeConverter().CanRead);
        }

        [Fact]
        public void CanWrite()
        {
            Assert.False(new OperationTypeConverter().CanWrite);
        }

        [Theory]
        [InlineData("")]
        [InlineData("42")]
        [InlineData("{ \"foo\": \"bar\" }")]
        [InlineData("[ 1, 2, 3 ]")]
        public void ReadJson_GivenInvalidToken_ThrowsJsonReaderException(string json)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.Equal(json != "", jsonReader.Read());
            Assert.Throws<JsonReaderException>(
                () => new OperationTypeConverter().ReadJson(
                    jsonReader,
                    typeof(OperationType),
                    null,
                    new JsonSerializer()));
        }

        [Theory]
        [InlineData("null", OperationType.Unknown)]
        [InlineData("\"ReIndeX\"", OperationType.Reindex)]
        [InlineData("\"NewJob\"", OperationType.Unknown)]
        [InlineData("\"Unknown\"", OperationType.Unknown)]
        public void ReadJson_GivenStringOrNullToken_ReturnOperationType(string json, OperationType expected)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            OperationType actual = (OperationType)(new OperationTypeConverter().ReadJson(
                    jsonReader,
                    typeof(OperationType),
                    null,
                    new JsonSerializer()));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WriteJson_GivenAnyInput_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => new OperationTypeConverter().WriteJson(
                    new JsonTextWriter(new StreamWriter(Stream.Null)),
                    OperationType.Reindex,
                    new JsonSerializer()));
        }
    }
}
