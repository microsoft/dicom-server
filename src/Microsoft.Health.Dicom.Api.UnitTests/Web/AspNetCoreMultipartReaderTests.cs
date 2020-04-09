// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Api.Web;
using Microsoft.Health.Dicom.Core.Web;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Web
{
    public class AspNetCoreMultipartReaderTests
    {
        private readonly ISeekableStreamConverter _seekableStreamConverter = Substitute.For<ISeekableStreamConverter>();

        [Fact]
        public void GivenInvalidContentType_WhenInitialized_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            Assert.Throws<UnsupportedMediaTypeException>(() => Create("invalid"));
        }

        [Fact]
        public void GivenNonMultipartRelatedContentType_WhenInitialized_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            Assert.Throws<UnsupportedMediaTypeException>(() => Create("multipart/form-data; boundary=123"));
        }

        [Fact]
        public void GivenMultipartRelatedContentTypeWithoutBoundary_WhenInitialized_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            Assert.Throws<UnsupportedMediaTypeException>(() => Create("multipart/form-data; type=\"application/dicom\""));
        }

        [Fact(Skip = "The test is not passing at the moment. Need to figure out what is going on with the stream.")]
        public async Task GivenASingleBodyPart_WhenRead_ThenCorrectMultipartBodyPartShouldBeReturned()
        {
            const string body = @"--123
Content-Type: application/dicom

content
--123--
";

            MemoryStream stream = null;
            StreamWriter writer = null;

            try
            {
                stream = new MemoryStream();
                writer = new StreamWriter(stream);

                await writer.WriteAsync(body);
                await writer.FlushAsync();

                stream.Seek(0, SeekOrigin.Begin);

                AspNetCoreMultipartReader aspNetCoreMultipartReader = Create("multipart/related; boundary=--123", stream);

                MultipartBodyPart result = await aspNetCoreMultipartReader.ReadNextBodyPartAsync(cancellationToken: default);

                Assert.NotNull(result);
                Assert.Equal("application/dicom", result.ContentType);
                Assert.True(result.Body.CanSeek);

                using (StreamReader reader = new StreamReader(result.Body))
                {
                    Assert.Equal("content", await reader.ReadToEndAsync());
                }
            }
            finally
            {
                if (writer != null)
                {
                    await writer.DisposeAsync();
                }

                if (stream != null)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        private AspNetCoreMultipartReader Create(string contentType, Stream body = null)
        {
            if (body == null)
            {
                body = new MemoryStream();
            }

            return new AspNetCoreMultipartReader(
                contentType,
                body,
                _seekableStreamConverter);
        }
    }
}
