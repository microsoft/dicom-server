// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Entries
{
    public class StreamOriginatedDicomInstanceEntryTests
    {
        private static readonly DicomDataset DefaultDicomDataset = new DicomDataset(
            new DicomUniqueIdentifier(DicomTag.SOPClassUID, "123"),
            new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "123"));

        [Fact]
        public async Task GivenAnInvalidStream_WhenDicomDatasetIsRequested_ThenInvalidDicomInstanceExceptionShouldBeThrown()
        {
            Stream stream = new MemoryStream();

            StreamOriginatedInstanceEntry instanceEntry = CreateStreamOriginatedDicomInstanceEntry(stream);

            Exception caughtException = null;

            try
            {
                await instanceEntry.GetDicomDatasetAsync(default);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<InvalidInstanceException>(caughtException);
        }

        [Fact]
        public async Task GivenAValidStream_WhenDicomDatasetIsRequested_ThenCorrectDatasetShouldBeReturned()
        {
            await using (Stream stream = await CreateStreamAsync(DefaultDicomDataset))
            {
                StreamOriginatedInstanceEntry instanceEntry = CreateStreamOriginatedDicomInstanceEntry(stream);

                DicomDataset dicomDataset = await instanceEntry.GetDicomDatasetAsync(default);

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

                StreamOriginatedInstanceEntry instanceEntry = CreateStreamOriginatedDicomInstanceEntry(stream);

                Stream readStream = await instanceEntry.GetStreamAsync(default);

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

        private StreamOriginatedInstanceEntry CreateStreamOriginatedDicomInstanceEntry(Stream stream)
            => new StreamOriginatedInstanceEntry(stream);
    }
}
