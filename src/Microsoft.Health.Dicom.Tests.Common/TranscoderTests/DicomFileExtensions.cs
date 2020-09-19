// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using Dicom;

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    public static class DicomFileExtensions
    {
        public static byte[] ToByteArray(this DicomFile dicomFile)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                dicomFile.Save(ms);
                return ms.ToArray();
            }
        }
    }
}
