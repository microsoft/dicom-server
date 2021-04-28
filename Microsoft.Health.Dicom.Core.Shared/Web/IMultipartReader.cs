// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Web
{
    /// <summary>
    /// Provides functionalities to read multipart message.
    /// </summary>
    public interface IMultipartReader
    {
        /// <summary>
        /// Read the next body part of a multipart message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="MultipartBodyPart"/> representing the read body part.</returns>
        Task<MultipartBodyPart> ReadNextBodyPartAsync(CancellationToken cancellationToken = default);
    }
}
