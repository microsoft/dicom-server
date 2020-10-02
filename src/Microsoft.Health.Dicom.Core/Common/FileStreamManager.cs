// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Dicom.Core.Common
{
    public class FileStreamManager
    {
#pragma warning disable CA1822 // Mark members as static
        public Stream GetStream()
#pragma warning restore CA1822 // Mark members as static
        {
            return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 1024 * 1024 * 5, FileOptions.DeleteOnClose);
        }
    }
}
