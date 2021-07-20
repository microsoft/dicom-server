// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class ExtendedQueryTagErrorsServiceTests
    {
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IExtendedQueryTagErrorsService _extendedQueryTagErrorsService;
        private readonly CancellationTokenSource _tokenSource;
        private readonly DateTime _definedNow;

        public ExtendedQueryTagErrorsServiceTests()
        {
            _extendedQueryTagErrorStore = Substitute.For<IExtendedQueryTagErrorStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _extendedQueryTagErrorsService = new ExtendedQueryTagErrorsService(_extendedQueryTagErrorStore, _dicomTagParser);
            _tokenSource = new CancellationTokenSource();
            _definedNow = DateTime.UtcNow;
        }

        [Fact]
        public async Task GivenValidInput_WhenAddingExtendedQueryTag_ThenShouldSucceed()
        {
            int tagKey = 7;
            int errorCode = 2;
            long watermark = 30;

            await _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                Arg.Is(tagKey),
                Arg.Is(errorCode),
                Arg.Is(watermark),
                Arg.Is(_definedNow),
                Arg.Is(_tokenSource.Token));

            var actual = await _extendedQueryTagErrorsService.AddExtendedQueryTagErrorAsync(
                tagKey,
                errorCode,
                watermark,
                _definedNow,
                _tokenSource.Token);

            await _extendedQueryTagErrorStore
                .Received(1)
                .AddExtendedQueryTagErrorAsync(
                Arg.Is(tagKey),
                Arg.Is(errorCode),
                Arg.Is(watermark),
                Arg.Is(_definedNow),
                Arg.Is(_tokenSource.Token));
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagDoesNotExist_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);

            Assert.Empty(response.ExtendedQueryTagErrors);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagHasNoError_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);

            // CHECK FOR RESPONSE MESSAGE?
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagExists_ThenReturnExtendedQueryTagErrorsList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            var expected = new List<ExtendedQueryTagError> { CreateExtendedQueryTagError("fake error message", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DateTime.UtcNow) };

            //expected.Select(x => new ExtendedQueryTagError() )
            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(expected);
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);

            Assert.Equal(expected, response.ExtendedQueryTagErrors);
        }

        private static ExtendedQueryTagError CreateExtendedQueryTagError(
            string errorMessage,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            DateTime timestamp)
        {
            return new ExtendedQueryTagError(timestamp, studyInstanceUid, seriesInstanceUid, sopInstanceUid, errorMessage);
        }
    }
}
