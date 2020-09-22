// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Web
{
    /// <summary>
    /// Provides functionality to convert stream into a seekable stream.
    /// </summary>
    public interface ISeekableStreamConverter
    {
        /// <summary>
        /// Converts the <paramref name="stream"/> into a seekable stream.
        /// </summary>
        /// <param name="stream">The stream to convert.</param>
        /// <param name="cancellationToken">The seekable stream.</param>
        /// <returns>A instance of seekable <see cref="Stream"/>.</returns>
        Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
