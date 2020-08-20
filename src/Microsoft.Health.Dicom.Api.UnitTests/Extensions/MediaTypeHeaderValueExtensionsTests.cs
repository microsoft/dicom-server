// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class MediaTypeHeaderValueExtensionsTests
    {
        [Fact]
        public void GivenNullHeader_WhenGetParameter_ShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => MediaTypeHeaderValueExtensions.GetParameter(headerValue: null, AcceptHeaderParameterNames.Type));
        }

        [Fact]
        public void GivenEmptyParameterName_WhenGetparameter_ShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => MediaTypeHeaderValueExtensions.GetParameter(new MediaTypeHeaderValue(KnownContentTypes.ApplicationDicom), parameterName: string.Empty));
        }

        [Fact]
        public void GivenValidHeader_WhenGetParameter_ShouldReturnCorrectValue()
        {
            string parameter = AcceptHeaderParameterNames.Type;
            string parameterValue = KnownContentTypes.ImageJpeg;
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(KnownContentTypes.MultipartRelated);
            headerValue.Parameters.Add(CreateNameValueHeaderValue(parameter, parameterValue));
            Assert.Equal(parameterValue, headerValue.GetParameter(parameter));
        }

        [Fact]
        public void GivenMultipleParameters_WhenGetParameter_ShouldReturnFirstValue()
        {
            string parameter = AcceptHeaderParameterNames.Type;
            string parameterValue1 = KnownContentTypes.ImageJpeg, parameterValue2 = KnownContentTypes.ApplicationOctetStream;
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(KnownContentTypes.MultipartRelated);
            headerValue.Parameters.Add(CreateNameValueHeaderValue(parameter, parameterValue1));
            headerValue.Parameters.Add(CreateNameValueHeaderValue(parameter, parameterValue2));
            Assert.Equal(parameterValue1, headerValue.GetParameter(parameter));
        }

        [Fact]
        public void GivenNoRequiredParameter_WhenGetParameter_ShouldReturnNull()
        {
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(KnownContentTypes.MultipartRelated);
            Assert.Equal(StringSegment.Empty, headerValue.GetParameter(AcceptHeaderParameterNames.Type));
        }

        [Fact]
        public void GivenParameterValueWithQuote_WhenGetParameterWithRemoveQuote_ShouldReturnValueWithoutQuote()
        {
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(KnownContentTypes.MultipartRelated);
            string parameter = AcceptHeaderParameterNames.Type;
            string parameterValue = KnownContentTypes.ApplicationOctetStream;
            headerValue.Parameters.Add(CreateNameValueHeaderValue(parameter, parameterValue));
            Assert.Equal(parameterValue, headerValue.GetParameter(parameter));
        }

        [Fact]
        public void GivenParameterValueWithQuote_WhenGetParameterWithoutRemoveQuote_ShouldReturnValueWithQuote()
        {
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(KnownContentTypes.MultipartRelated);
            string parameter = AcceptHeaderParameterNames.Type;
            string parameterValue = KnownContentTypes.ApplicationOctetStream;
            string parameterValueWithQuote = QuoteText(parameterValue);
            headerValue.Parameters.Add(CreateNameValueHeaderValue(parameter, parameterValue));
            Assert.Equal(parameterValueWithQuote, headerValue.GetParameter(parameter, tryRemoveQuotes: false));
        }

        [Fact]
        public void GivenSinglePartHeader_WhenGetAcceptHeader_ShouldSucceed()
        {
            string mediaType = KnownContentTypes.ApplicationDicom;
            string transferSyntax = DicomTransferSyntaxUids.Original;
            double quality = 0.9;
            MediaTypeHeaderValue headerValue = CreateMediaTypeHeaderValue(mediaType, string.Empty, transferSyntax, quality);
            AcceptHeader acceptHeader = headerValue.ToAcceptHeader();
            Assert.Equal(PayloadTypes.SinglePart, acceptHeader.PayloadType);
            Assert.Equal(mediaType, acceptHeader.MediaType);
            Assert.Equal(transferSyntax, acceptHeader.TransferSyntax);
            Assert.Equal(quality, acceptHeader.Quality);
        }

        [Fact]
        public void GivenMultiPartRelatedHeader_WhenGetAcceptHeader_ShouldSucceed()
        {
            string type = KnownContentTypes.ApplicationOctetStream;
            string transferSyntax = DicomTransferSyntaxUids.Original;

            double quality = 0.9;
            MediaTypeHeaderValue headerValue = CreateMediaTypeHeaderValue(KnownContentTypes.MultipartRelated, type, transferSyntax, quality);
            AcceptHeader acceptHeader = headerValue.ToAcceptHeader();
            Assert.Equal(PayloadTypes.MultipartRelated, acceptHeader.PayloadType);
            Assert.Equal(type, acceptHeader.MediaType);
            Assert.Equal(transferSyntax, acceptHeader.TransferSyntax);
            Assert.Equal(quality, acceptHeader.Quality);
        }

        [Fact]
        public void GivenMultiPartButNotRelatedHeader_WhenGetAcceptHeader_ShouldSucceed()
        {
            string mediaType = "multipart/form-data";
            string type = KnownContentTypes.ApplicationDicom;
            string transferSyntax = DicomTransferSyntaxUids.Original;
            double quality = 0.9;
            MediaTypeHeaderValue headerValue = CreateMediaTypeHeaderValue(mediaType, type, transferSyntax, quality);
            AcceptHeader acceptHeader = headerValue.ToAcceptHeader();
            Assert.Equal(PayloadTypes.SinglePart, acceptHeader.PayloadType);
            Assert.Equal(mediaType, acceptHeader.MediaType);
            Assert.Equal(transferSyntax, acceptHeader.TransferSyntax);
            Assert.Equal(quality, acceptHeader.Quality);
        }

        private MediaTypeHeaderValue CreateMediaTypeHeaderValue(string mediaType, string type, string transferSyntax, double? quality)
        {
            MediaTypeHeaderValue result = new MediaTypeHeaderValue(mediaType);
            if (!string.IsNullOrEmpty(type))
            {
                result.Parameters.Add(CreateNameValueHeaderValue(AcceptHeaderParameterNames.Type, type, quoteValue: true));
            }

            if (!string.IsNullOrEmpty(transferSyntax))
            {
                result.Parameters.Add(CreateNameValueHeaderValue(AcceptHeaderParameterNames.TransferSyntax, transferSyntax, quoteValue: false));
            }

            if (quality.HasValue)
            {
                result.Parameters.Add(CreateNameValueHeaderValue(AcceptHeaderParameterNames.Quality, quality.ToString(), quoteValue: false));
            }

            return result;
        }

        private string QuoteText(string text)
        {
            return $"\"{text}\"";
        }

        private NameValueHeaderValue CreateNameValueHeaderValue(string key, string value, bool quoteValue = true)
        {
            return new NameValueHeaderValue(key, quoteValue ? QuoteText(value) : value);
        }
    }
}
