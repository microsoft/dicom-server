// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Api.Web;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Web;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using NotSupportedException = Microsoft.Health.Dicom.Core.Exceptions.NotSupportedException;

namespace Microsoft.Health.Dicom.Api.UnitTests.Web
{
    public class AspNetCoreMultipartReaderTests
    {
        private const string DefaultContentType = "multipart/related; boundary=+b+";
        private const string DefaultBodyPartSeparator = "--+b+";
        private const string DefaultBodyPartFinalSeparator = "--+b+--";

        private readonly ISeekableStreamConverter _seekableStreamConverter;

        public AspNetCoreMultipartReaderTests()
        {
            _seekableStreamConverter = new SeekableStreamConverter(Substitute.For<IHttpContextAccessor>(), CreateStoreConfiguration());
        }

        [Fact]
        public void GivenInvalidContentType_WhenInitializing_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            Assert.Throws<UnsupportedMediaTypeException>(() => Create("invalid"));
        }

        [Fact]
        public void GivenNonMultipartRelatedContentType_WhenInitializing_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            Assert.Throws<UnsupportedMediaTypeException>(() => Create("multipart/form-data; boundary=123"));
        }

        [Fact]
        public void GivenMultipartRelatedContentTypeWithoutBoundary_WhenInitializing_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            Assert.Throws<UnsupportedMediaTypeException>(() => Create("multipart/form-data; type=\"application/dicom\""));
        }

        [Fact]
        public async Task GivenASingleBodyPartWithContentType_WhenReading_ThenCorrectMultipartBodyPartShouldBeReturned()
        {
            string body = GenerateBody(
                DefaultBodyPartSeparator,
                $"Content-Type: application/dicom",
                string.Empty,
                "content",
                DefaultBodyPartFinalSeparator);

            await ExecuteAndValidateAsync(
                body,
                DefaultContentType,
                async bodyPart => await ValidateMultipartBodyPartAsync("application/dicom", "content", bodyPart));
        }

        [Theory]
        [InlineData("type=text", "text")]
        [InlineData("type=\"text/plain\"", "text/plain")]
        public async Task GivenASingleBodyPartWithoutContentTypeAndRequestContentTypeWithTypeParameter_WhenReading_ThenTypeParameterFromRequestContentTypeShouldBeUsed(string typeParameterValue, string expectedTypeValue)
        {
            string requestContentType = $"multipart/related; {typeParameterValue}; boundary=+b+";
            string body = GenerateBody(
                DefaultBodyPartSeparator,
                string.Empty,
                "content",
                DefaultBodyPartFinalSeparator);

            await ExecuteAndValidateAsync(
                body,
                requestContentType,
                async bodyPart => await ValidateMultipartBodyPartAsync(expectedTypeValue, "content", bodyPart));
        }

        [Fact]
        public async Task GivenASingleBodyPartWithContentTypeAndRequestContentTypeWithTypeParameter_WhenReading_ThenContentTypeFromBodyPartShouldBeUsed()
        {
            const string requestContentType = "multipart/related; type=\"text/plain\"; boundary=+b+";
            string body = GenerateBody(
                DefaultBodyPartSeparator,
                "Content-Type: application/dicom",
                string.Empty,
                "content",
                DefaultBodyPartFinalSeparator);

            await ExecuteAndValidateAsync(
                body,
                requestContentType,
                async bodyPart => await ValidateMultipartBodyPartAsync("application/dicom", "content", bodyPart));
        }

        [Fact]
        public async Task GivenMultipeBodyPartsWithoutContentTypeAndRequestContentTypeWithTypeParameter_WhenReading_ThenContentTypeFromBodyPartShouldBeUsedOnlyForFirstBodyPart()
        {
            const string requestContentType = "multipart/related; type=\"text/plain\"; boundary=+b+";
            string body = GenerateBody(
                DefaultBodyPartSeparator,
                string.Empty,
                "content",
                DefaultBodyPartSeparator,
                string.Empty,
                "content2",
                DefaultBodyPartFinalSeparator);

            await ExecuteAndValidateAsync(
                body,
                requestContentType,
                async bodyPart => await ValidateMultipartBodyPartAsync("text/plain", "content", bodyPart),
                async bodyPart => await ValidateMultipartBodyPartAsync(null, "content2", bodyPart));
        }

        [Fact]
        public async Task GivenMultipeBodyParts_WhenReading_ThenCorrectMultipartBodyPartShouldBeReturned()
        {
            const string requestContentType = "multipart/related; type=\"text/plain\"; boundary=+123+";
            string body = GenerateBody(
                "--+123+",
                string.Empty,
                "content",
                "--+123+",
                "Content-Type: application/dicom+json",
                string.Empty,
                "content2",
                "--+123+--");

            await ExecuteAndValidateAsync(
                body,
                requestContentType,
                async bodyPart => await ValidateMultipartBodyPartAsync("text/plain", "content", bodyPart),
                async bodyPart => await ValidateMultipartBodyPartAsync("application/dicom+json", "content2", bodyPart));
        }

        [Fact]
        public async Task GivenMissingMultipartBodyPartException_WhenReading_NoMoreSectionShouldBeReturned()
        {
            string body = GenerateBody(
                DefaultBodyPartSeparator,
                "Content-Type: application/dicom",
                string.Empty,
                "content",
                DefaultBodyPartSeparator,
                "Content-Type: application/dicom+json",
                string.Empty,
                "content2",
                DefaultBodyPartFinalSeparator);

            ISeekableStreamConverter seekableStreamConverter = Substitute.For<ISeekableStreamConverter>();

            seekableStreamConverter.ConvertAsync(default, default).ThrowsForAnyArgs(new InvalidMultipartBodyPartException(new IOException()));

            using (MemoryStream stream = await CreateMemoryStream(body))
            {
                AspNetCoreMultipartReader aspNetCoreMultipartReader = Create(DefaultContentType, stream, seekableStreamConverter);

                MultipartBodyPart result = await aspNetCoreMultipartReader.ReadNextBodyPartAsync(cancellationToken: default);

                Assert.Null(result);
            }
        }

        [Fact]
        public void GivenStartParameter_WhenReading_ThenDicomNotSupportedExceptionShouldBeThrown()
        {
            const string requestContentType = "multipart/related; type=\"application/dicom\"; start=\"somewhere\"; boundary=+b+";

            Assert.Throws<NotSupportedException>(() => Create(requestContentType));
        }

        private AspNetCoreMultipartReader Create(string contentType, Stream body = null, ISeekableStreamConverter seekableStreamConverter = null)
        {
            if (body == null)
            {
                body = new MemoryStream();
            }

            if (seekableStreamConverter == null)
            {
                seekableStreamConverter = _seekableStreamConverter;
            }

            return new AspNetCoreMultipartReader(
                contentType,
                body,
                seekableStreamConverter,
                CreateStoreConfiguration());
        }

        private IOptions<StoreConfiguration> CreateStoreConfiguration()
        {
            var configuration = Substitute.For<IOptions<StoreConfiguration>>();
            configuration.Value.Returns(new StoreConfiguration
            {
                MaxAllowedDicomFileSize = 1000000,
            });
            return configuration;
        }

        private async Task<MemoryStream> CreateMemoryStream(string content)
        {
            MemoryStream stream = new MemoryStream();

            using (StreamWriter writer = new StreamWriter(stream, leaveOpen: true))
            {
                await writer.WriteAsync(content);
            }

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private async Task ExecuteAndValidateAsync(string body, string requestContentType, params Func<MultipartBodyPart, Task>[] validators)
        {
            using (MemoryStream stream = await CreateMemoryStream(body))
            {
                AspNetCoreMultipartReader aspNetCoreMultipartReader = Create(requestContentType, stream);

                MultipartBodyPart result = null;

                foreach (Func<MultipartBodyPart, Task> validator in validators)
                {
                    result = await aspNetCoreMultipartReader.ReadNextBodyPartAsync(cancellationToken: default);

                    await validator(result);
                }

                result = await aspNetCoreMultipartReader.ReadNextBodyPartAsync(cancellationToken: default);

                Assert.Null(result);
            }
        }

        private async Task ValidateMultipartBodyPartAsync(string expectedContentType, string expectedBody, MultipartBodyPart actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedContentType, actual.ContentType);

            using (StreamReader reader = new StreamReader(actual.SeekableStream))
            {
                Assert.Equal(expectedBody, await reader.ReadToEndAsync());
            }
        }

        private string GenerateBody(params string[] lines)
        {
            // Body part requires \r\n as separator per RFC2616.
            return string.Join("\r\n", lines);
        }
    }
}
