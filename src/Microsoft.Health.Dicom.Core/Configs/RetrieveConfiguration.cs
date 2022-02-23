// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class RetrieveConfiguration
    {
        /// <summary>
        /// Maximum dicom file supported for getting frames and transcoding
        /// </summary>
        public long MaxDicomFileSize { get; } = 1024 * 1024 * 100; //100 MB

        /// <summary>
        /// Response uses lazy streams that copy data from storage JIT into a buffer and then to the output stream
        /// This is the size of the buffer
        /// </summary>
        public int LazyResponseStreamBufferSize { get; } = 1024 * 1024 * 4; //4 MB
    }
}
