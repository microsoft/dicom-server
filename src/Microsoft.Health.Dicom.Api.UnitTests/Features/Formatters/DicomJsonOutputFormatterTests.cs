// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Health.Dicom.Api.Features.ContentTypes;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Formatters
{
    public class DicomJsonOutputFormatterTests
    {
        [Theory]
        [InlineData(typeof(JObject))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DicomItem))]
        [InlineData(null)]
        public void GivenAnInvalidDicomObjectAndJsonContentType_WhenCheckingCanWrite_ThenFalseShouldBeReturned(Type modelType)
        {
            bool result = CanWrite(modelType, KnownContentTypes.JsonContentType);
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(DicomDataset))]
        [InlineData(typeof(IEnumerable<DicomDataset>))]
        [InlineData(typeof(IList<DicomDataset>))]
        [InlineData(typeof(IReadOnlyCollection<DicomDataset>))]
        public void GivenAValidDicomObjectAndContentType_WhenCheckingCanWrite_ThenTrueShouldBeReturned(Type modelType)
        {
            bool result = CanWrite(modelType, KnownContentTypes.JsonContentType);
            Assert.True(result);
        }

        private bool CanWrite(Type modelType, string contentType)
        {
            var formatter = new DicomJsonOutputFormatter();

            var defaultHttpContext = new DefaultHttpContext();
            defaultHttpContext.Request.ContentType = contentType;

            var result = formatter.CanWriteResult(
                new OutputFormatterWriteContext(
                    new DefaultHttpContext(),
                    Substitute.For<Func<Stream, Encoding, TextWriter>>(),
                    modelType,
                    new object()));

            return result;
        }
    }
}
