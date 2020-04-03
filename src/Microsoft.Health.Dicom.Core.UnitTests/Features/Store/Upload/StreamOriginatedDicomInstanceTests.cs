// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Upload;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Upload
{
    public class StreamOriginatedDicomInstanceTests
    {
        private static readonly DicomDataset DefaultDicomDataset = new DicomDataset(
            new DicomUniqueIdentifier(DicomTag.SOPClassUID, "123"),
            new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "123"));

        [Fact]
        public async Task GivenAnInvalidStream_WhenDicomDatasetIsRequested_ThenInvalidDicomInstanceExceptionShouldBeThrown()
        {
            Stream stream = new MemoryStream();

            StreamOriginatedDicomInstance dicomInstance = CreateStreamOriginatedDicomInstance(stream);

            Exception caughtException = null;

            try
            {
                await dicomInstance.GetDicomDatasetAsync(default);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<InvalidDicomInstanceException>(caughtException);
        }

        [Fact]
        public async Task GivenAValidStream_WhenDicomDatasetIsRequested_ThenCorrectDatasetShouldBeReturned()
        {
            await using (Stream stream = await CreateStreamAsync(DefaultDicomDataset))
            {
                StreamOriginatedDicomInstance dicomInstance = CreateStreamOriginatedDicomInstance(stream);

                DicomDataset dicomDataset = await dicomInstance.GetDicomDatasetAsync(default);

                Assert.NotNull(dicomDataset);
                Assert.Equal(DefaultDicomDataset, dicomDataset);
            }
        }

        [Fact]
        public async Task GivenAValidStream_WhenStreamIsRetrieved_ThenStreamShouldBeRewindToBeginning()
        {
            await using (Stream stream = await CreateStreamAsync(DefaultDicomDataset))
            {
                // Force to move to the end of stream.
                stream.Seek(0, SeekOrigin.End);

                StreamOriginatedDicomInstance dicomInstance = CreateStreamOriginatedDicomInstance(stream);

                Stream readStream = await dicomInstance.GetStreamAsync(default);

                Assert.NotNull(readStream);
                Assert.Equal(0, readStream.Position);
            }
        }

        private async Task<Stream> CreateStreamAsync(DicomDataset dicomDataset)
        {
            var dicomFile = new DicomFile(dicomDataset);

            var stream = new MemoryStream();

            await dicomFile.SaveAsync(stream);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private StreamOriginatedDicomInstance CreateStreamOriginatedDicomInstance(Stream stream)
            => new StreamOriginatedDicomInstance(stream);
    }
}
