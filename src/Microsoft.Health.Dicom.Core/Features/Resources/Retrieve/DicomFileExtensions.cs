// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dicom;
using Dicom.Imaging;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public static class DicomFileExtensions
    {
        public static void ValidateHasFrames(this DicomFile dicomFile, IEnumerable<int> frames)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            DicomDataset dataset = dicomFile.Dataset;

            // Validate the dataset has the correct DICOM tags.
            if (!dataset.Contains(DicomTag.BitsAllocated) ||
                !dataset.Contains(DicomTag.Columns) ||
                !dataset.Contains(DicomTag.Rows) ||
                !dataset.Contains(DicomTag.PixelData))
            {
                throw new DataStoreException(HttpStatusCode.NotFound);
            }

            // Note: We look for any frame value that is less than zero, or greater than number of frames.
            var pixelData = DicomPixelData.Create(dataset);
            var missingFrames = frames.Where(x => x >= pixelData.NumberOfFrames || x < 0).ToArray();

            // If any missing frames, throw not found exception for the specific frames not found.
            if (missingFrames.Length > 0)
            {
                throw new DataStoreException(HttpStatusCode.NotFound, new ArgumentException($"The frame(s) '{string.Join(", ", missingFrames)}' do not exist.", nameof(frames)));
            }
        }
    }
}
