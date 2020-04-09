// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Provides functionality to create a new instance of <see cref="AspNetCoreMultipartReader"/>.
    /// </summary>
    internal class AspNetCoreMultipartReaderFactory : IMultipartReaderFactory
    {
        private readonly ISeekableStreamConverter _seekableStreamConverter;

        public AspNetCoreMultipartReaderFactory(
            ISeekableStreamConverter seekableStreamConverter)
        {
            EnsureArg.IsNotNull(seekableStreamConverter, nameof(seekableStreamConverter));

            _seekableStreamConverter = seekableStreamConverter;
        }

        /// <inheritdoc />
        public IMultipartReader Create(string contentType, Stream body)
        {
            return new AspNetCoreMultipartReader(
                contentType,
                body,
                _seekableStreamConverter);
        }
    }
}
