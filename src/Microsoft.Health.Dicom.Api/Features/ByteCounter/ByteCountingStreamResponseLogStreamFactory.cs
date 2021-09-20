// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Dicom.Api.Features.ByteCounter
{
    public class ByteCountingStreamResponseLogStreamFactory : IResponseLogStreamFactory
    {
        /// <inheritdoc/>
        public ByteCountingStream CreateByteCountingResponseLogStream(Stream stream)
        {
            return new ByteCountingStream(stream);
        }
    }
}
