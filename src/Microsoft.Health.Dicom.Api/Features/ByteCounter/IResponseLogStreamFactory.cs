// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Dicom.Api.Features.ByteCounter
{
    public interface IResponseLogStreamFactory
    {
        /// <summary>
        /// Creates a <see cref="ByteCountingStream"/>
        /// </summary>
        /// <param name="stream">An underlying stream.</param>
        /// <returns>A byte counting stream that wraps the specified underlying stream.</returns>
        ByteCountingStream CreateByteCountingResponseLogStream(Stream stream);
    }
}
