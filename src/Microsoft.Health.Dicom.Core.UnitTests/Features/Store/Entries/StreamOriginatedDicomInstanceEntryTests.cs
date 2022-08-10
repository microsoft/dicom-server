// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Entries;

public class StreamOriginatedDicomInstanceEntryTests
{
    private static readonly DicomDataset DefaultDicomDataset = new DicomDataset(
        new DicomUniqueIdentifier(DicomTag.SOPClassUID, "123"),
        new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "123"));

    [Fact]
    public async Task GivenAnInvalidStream_WhenDicomDatasetIsRequested_ThenInvalidInstanceExceptionShouldBeThrown()
    {
        Stream stream = new MemoryStream();

        StreamOriginatedDicomInstanceEntry dicomInstanceEntry = CreateStreamOriginatedDicomInstanceEntry(stream);
        await Assert.ThrowsAsync<InvalidInstanceException>(() => dicomInstanceEntry.GetDicomDatasetAsync(default).AsTask());
    }

    [Fact]
    public async Task GivenAValidStream_WhenDicomDatasetIsRequested_ThenCorrectDatasetShouldBeReturned()
    {
        await using (Stream stream = await CreateStreamAsync(DefaultDicomDataset))
        {
            StreamOriginatedDicomInstanceEntry dicomInstanceEntry = CreateStreamOriginatedDicomInstanceEntry(stream);

            DicomDataset dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(default);

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

            StreamOriginatedDicomInstanceEntry dicomInstanceEntry = CreateStreamOriginatedDicomInstanceEntry(stream);

            Stream readStream = await dicomInstanceEntry.GetStreamAsync(default);

            Assert.NotNull(readStream);
            Assert.Equal(0, readStream.Position);
        }
    }

    private static async Task<Stream> CreateStreamAsync(DicomDataset dicomDataset)
    {
        var dicomFile = new DicomFile(dicomDataset);

        var stream = new MemoryStream();

        await dicomFile.SaveAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }

    private static StreamOriginatedDicomInstanceEntry CreateStreamOriginatedDicomInstanceEntry(Stream stream)
        => new StreamOriginatedDicomInstanceEntry(stream);
}
