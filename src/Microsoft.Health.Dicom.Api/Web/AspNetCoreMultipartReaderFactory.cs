// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Provides functionality to create a new instance of <see cref="AspNetCoreMultipartReader"/>.
    /// </summary>
    internal class AspNetCoreMultipartReaderFactory : IMultipartReaderFactory
    {
        private readonly ISeekableStreamConverter _seekableStreamConverter;
        private readonly IOptions<StoreConfiguration> _storeConfiguration;

        public AspNetCoreMultipartReaderFactory(
            ISeekableStreamConverter seekableStreamConverter,
            IOptions<StoreConfiguration> storeConfiguration)
        {
            EnsureArg.IsNotNull(seekableStreamConverter, nameof(seekableStreamConverter));
            EnsureArg.IsNotNull(storeConfiguration?.Value, nameof(storeConfiguration));

            _seekableStreamConverter = seekableStreamConverter;
            _storeConfiguration = storeConfiguration;
        }

        /// <inheritdoc />
        public IMultipartReader Create(string contentType, Stream body)
        {
            return new AspNetCoreMultipartReader(
                contentType,
                body,
                _seekableStreamConverter,
                _storeConfiguration);
        }
    }
}
