// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.Serialization
{
    public class OperationTypeConverterTests
    {
        [Fact]
        public void GivenOperationTypeConverter_WhenCheckingRead_ThenReturnTrue()
        {
            Assert.True(new OperationTypeConverter().CanRead);
        }

        [Fact]
        public void GivenOperationTypeConverter_WhenCheckingWrite_ThenReturnFalse()
        {
            Assert.False(new OperationTypeConverter().CanWrite);
        }

        [Theory]
        [InlineData("")]
        [InlineData("42")]
        [InlineData("{ \"foo\": \"bar\" }")]
        [InlineData("[ 1, 2, 3 ]")]
        public void GivenInvalidToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
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
        [InlineData("\"ReindexInstancesAsync\"", OperationType.Reindex)]
        [InlineData("\"NewJob\"", OperationType.Unknown)]
        [InlineData("\"Unknown\"", OperationType.Unknown)]
        public void GivenStringOrNullToken_WhenReadingJson_ThenReturnOperationType(string json, OperationType expected)
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
        public void GivenAnyInput_WhenWritingJson_ThenThrowNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => new OperationTypeConverter().WriteJson(
                    new JsonTextWriter(new StreamWriter(Stream.Null)),
                    OperationType.Reindex,
                    new JsonSerializer()));
        }
    }
}
