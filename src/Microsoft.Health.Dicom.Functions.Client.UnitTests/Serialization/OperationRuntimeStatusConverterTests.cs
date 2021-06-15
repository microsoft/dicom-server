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
    public class OperationRuntimeStatusConverterTests
    {
        [Fact]
        public void GivenOperationRuntimeStatusConverter_WhenCheckingRead_ThenReturnTrue()
        {
            Assert.True(new OperationRuntimeStatusConverter().CanRead);
        }

        [Fact]
        public void GivenOperationRuntimeStatusConverter_WhenCheckingWrite_ThenReturnFalse()
        {
            Assert.False(new OperationRuntimeStatusConverter().CanWrite);
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
                () => new OperationRuntimeStatusConverter().ReadJson(
                    jsonReader,
                    typeof(OperationRuntimeStatus),
                    null,
                    new JsonSerializer()));
        }

        [Theory]
        [InlineData("null", OperationRuntimeStatus.Unknown)]
        [InlineData("\"Pending\"", OperationRuntimeStatus.Pending)]
        [InlineData("\"running\"", OperationRuntimeStatus.Running)]
        [InlineData("\"ContinuedAsNew\"", OperationRuntimeStatus.Running)]
        [InlineData("\"completed\"", OperationRuntimeStatus.Completed)]
        [InlineData("\"Failed\"", OperationRuntimeStatus.Failed)]
        [InlineData("\"CANCELED\"", OperationRuntimeStatus.Canceled)]
        [InlineData("\"TerMINated\"", OperationRuntimeStatus.Canceled)]
        [InlineData("\"Unknown\"", OperationRuntimeStatus.Unknown)]
        [InlineData("\"Something Else\"", OperationRuntimeStatus.Unknown)]
        public void GivenStringOrNullToken_WhenReadingJson_ThenReturnOperationStatus(string json, OperationRuntimeStatus expected)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            OperationRuntimeStatus actual = (OperationRuntimeStatus)(new OperationRuntimeStatusConverter().ReadJson(
                    jsonReader,
                    typeof(OperationRuntimeStatus),
                    null,
                    new JsonSerializer()));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GivenAnyInput_WhenWritingJson_ThenThrowNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => new OperationRuntimeStatusConverter().WriteJson(
                    new JsonTextWriter(new StreamWriter(Stream.Null)),
                    OperationRuntimeStatus.Running,
                    new JsonSerializer()));
        }
    }
}
