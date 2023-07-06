// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Writer;
using FellowOakDicom.IO;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public static class DicomFileExtensions
{
    public static DicomPixelData GetPixelDataAndValidateFrames(this DicomFile dicomFile, IEnumerable<int> frames)
    {
        var pixelData = GetPixelData(dicomFile);
        ValidateFrames(pixelData, frames);

        return pixelData;
    }

    public static bool TryGetPixelData(this DicomDataset dataset, out DicomPixelData dicomPixelData)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        dicomPixelData = null;

        // Validate the dataset has the correct DICOM tags.
        if (!dataset.Contains(DicomTag.BitsAllocated) ||
            !dataset.Contains(DicomTag.Columns) ||
            !dataset.Contains(DicomTag.Rows) ||
            !dataset.Contains(DicomTag.PixelData))
        {
            return false;
        }
        dicomPixelData = DicomPixelData.Create(dataset);
        return true;
    }

    public static DicomPixelData GetPixelData(this DicomFile dicomFile)
    {
        EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
        DicomDataset dataset = dicomFile.Dataset;

        // Validate the dataset has the correct DICOM tags.
        if (!TryGetPixelData(dataset, out DicomPixelData dicomPixelData))
        {
            throw new FrameNotFoundException();
        }

        return dicomPixelData;
    }

    public static void ValidateFrames(DicomPixelData pixelData, IEnumerable<int> frames)
    {
        // Note: We look for any frame value that is less than zero, or greater than number of frames.
        var missingFrames = frames.Where(x => x >= pixelData.NumberOfFrames || x < 0).ToArray();

        // If any missing frames, throw not found exception for the specific frames not found.
        if (missingFrames.Length > 0)
        {
            throw new FrameNotFoundException();
        }
    }

    /// <summary>
    /// Given a dicom file, the method will return the length of the dataset.
    /// </summary>
    /// <param name="dcmFile"> Dicom file</param>
    /// <param name="recyclableMemoryStreamManager">RecyclableMemoryStreamManager to get Memory stream</param>
    /// <returns>Dataset size</returns>
    public static async Task<long> GetByteLengthAsync(this DicomFile dcmFile, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
    {
        EnsureArg.IsNotNull(dcmFile, nameof(dcmFile));
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

        DicomDataset dataset = dcmFile.Dataset;

        var writeOptions = new DicomWriteOptions();
        using MemoryStream resultStream = recyclableMemoryStreamManager.GetStream(tag: nameof(GetByteLengthAsync));

        var target = new StreamByteTarget(resultStream);
        var writer = new DicomFileWriter(writeOptions);
        await writer.WriteAsync(target, dcmFile.FileMetaInfo, dataset);

        return resultStream.Length;
    }
}
