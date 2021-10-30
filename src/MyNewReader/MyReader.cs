// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;

namespace MyNewReader
{
    public static class MyReader
    {
        public static async Task<Dictionary<int, Tuple<long, long>>> GetFramesOffsetAsync(Stream stream)
        {
            var dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand, 1000);

            var pixelData = dicomFile.Dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
            int numberOfFrames = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.NumberOfFrames, 1);

            if (numberOfFrames <= 1)
            {
                return null;
            }

            if (pixelData is DicomFragmentSequence pixelDataFragment && pixelDataFragment.OffsetTable != null && numberOfFrames == pixelDataFragment.Fragments.Count)
            {

                var framesRange = new Dictionary<int, Tuple<long, long>>();
                for (int i = 0; i < pixelDataFragment.Fragments.Count; i++)
                {
                    var fragment = pixelDataFragment.Fragments[i];
                    if (fragment is StreamByteBuffer streamByteBuffer)
                    {
                        framesRange.Add(i, Tuple.Create(streamByteBuffer.Position, streamByteBuffer.Size));
                    }
                    else if (fragment is FileByteBuffer fileByteBuffer)
                    {
                        framesRange.Add(i, Tuple.Create(fileByteBuffer.Position, fileByteBuffer.Size));
                    }
                }
                return framesRange;
            }

            return null;
        }
    }
}
