// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Operations.Serialization
{
    public class OperationStatusConverterTests
    {
        [Fact]
        public void CanRead()
        {
            Assert.True(new OperationStatusConverter().CanRead);
        }

        [Fact]
        public void CanWrite()
        {
            Assert.False(new OperationStatusConverter().CanWrite);
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
                () => new OperationStatusConverter().ReadJson(
                    jsonReader,
                    typeof(OperationStatus),
                    null,
                    new JsonSerializer()));
        }

        [Theory]
        [InlineData("null", OperationStatus.Unknown)]
        [InlineData("\"Pending\"", OperationStatus.Pending)]
        [InlineData("\"running\"", OperationStatus.Running)]
        [InlineData("\"ContinuedAsNew\"", OperationStatus.Running)]
        [InlineData("\"completed\"", OperationStatus.Completed)]
        [InlineData("\"Failed\"", OperationStatus.Failed)]
        [InlineData("\"CANCELED\"", OperationStatus.Canceled)]
        [InlineData("\"TerMINated\"", OperationStatus.Canceled)]
        [InlineData("\"Unknown\"", OperationStatus.Unknown)]
        [InlineData("\"Something Else\"", OperationStatus.Unknown)]
        public void ReadJson_GivenStringOrNullToken_ReturnOperationStatus(string json, OperationStatus expected)
        {
            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            Assert.True(jsonReader.Read());
            OperationStatus actual = (OperationStatus)(new OperationStatusConverter().ReadJson(
                    jsonReader,
                    typeof(OperationStatus),
                    null,
                    new JsonSerializer()));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WriteJson_GivenAnyInput_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => new OperationStatusConverter().WriteJson(
                    new JsonTextWriter(new StreamWriter(Stream.Null)),
                    OperationStatus.Running,
                    new JsonSerializer()));
        }
    }
}
