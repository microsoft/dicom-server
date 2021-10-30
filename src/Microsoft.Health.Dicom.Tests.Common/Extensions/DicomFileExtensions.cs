// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class DicomFileExtensions
    {
        public static byte[] ToByteArray(this DicomFile dicomFile)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
                dicomFile.Save(ms);
                return ms.ToArray();
            }
        }
    }
}
