// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ToByteArray(this Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                EnsureArg.IsNotNull(stream, nameof(stream));
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
