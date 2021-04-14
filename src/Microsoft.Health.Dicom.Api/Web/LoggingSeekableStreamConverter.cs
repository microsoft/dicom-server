// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Provides logging for <see cref="ISeekableStreamConverter"/>.
    /// </summary>
    internal class LoggingSeekableStreamConverter : ISeekableStreamConverter
    {
        private static readonly Action<ILogger, Exception> LogMissingMultipartBodyPartDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "Unexpected end of the stream. This is likely due to the request being multipart but has no section.");

        private static readonly Action<ILogger, Exception> LogUnhandledExceptionDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "Unhandled exception while reading stream.");

        private readonly ISeekableStreamConverter _seekableStreamConverter;
        private readonly ILogger _logger;

        public LoggingSeekableStreamConverter(
            ISeekableStreamConverter seekableStreamConverter,
            ILogger<LoggingSeekableStreamConverter> logger)
        {
            EnsureArg.IsNotNull(seekableStreamConverter, nameof(seekableStreamConverter));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _seekableStreamConverter = seekableStreamConverter;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                return await _seekableStreamConverter.ConvertAsync(stream, cancellationToken);
            }
            catch (InvalidMultipartBodyPartException ex)
            {
                LogMissingMultipartBodyPartDelegate(_logger, ex);

                throw;
            }
            catch (Exception ex)
            {
                LogUnhandledExceptionDelegate(_logger, ex);

                throw;
            }
        }
    }
}
