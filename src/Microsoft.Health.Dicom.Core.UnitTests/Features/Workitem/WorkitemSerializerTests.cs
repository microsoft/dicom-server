// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Web;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class WorkitemSerializerTests
    {
        private readonly WorkitemSerializer _target;

        public WorkitemSerializerTests()
        {
            _target = new WorkitemSerializer();
        }

        [Fact]
        public async Task GivenUnsupportedContentType_WhenHandled_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => _target.DeserializeAsync<DicomDataset>(Stream.Null, @"invalid"));
        }

        [Fact]
        public async Task GivenNullStream_WhenDeserialized_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _target.DeserializeAsync<DicomDataset>(null, KnownContentTypes.ApplicationJson));
        }

        [Fact]
        public async Task GivenValidStream_WhenDeserialized_ReturnsDicomDataset()
        {
            using (var stream = new MemoryStream(await GetWorkitemBytesAsync()))
            {
                var datasets = await _target.DeserializeAsync<IEnumerable<DicomDataset>>(stream, KnownContentTypes.ApplicationJson);

                Assert.NotNull(datasets);
                Assert.True(datasets.Any());
            }
        }

        private static async Task<byte[]> GetWorkitemBytesAsync()
        {
            var stream = new MemoryStream();
            using (var streamWriter = new StreamWriter(stream))
            {
                await streamWriter.WriteAsync("[{\"00081080\": {\"vr\": \"LO\"},\"00081084\": {\"vr\": \"SQ\"}}]");
                await streamWriter.FlushAsync();

                stream.Position = 0;
            }

            return stream.ToArray();
        }
    }
}
