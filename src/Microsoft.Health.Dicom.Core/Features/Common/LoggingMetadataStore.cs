// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public class LoggingMetadataStore : IMetadataStore
    {
        private static readonly Action<ILogger, string, Exception> LogStoreInstanceMetadataDelegate =
               LoggerMessage.Define<string>(
                   LogLevel.Debug,
                   default,
                   "Storing DICOM instance metadata file with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, string, Exception> LogDeleteInstanceMetadataDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance metadata file with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, string, Exception> LogGetInstanceMetadataDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                default,
                "Getting the DICOM instance metadata file with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, Exception> LogOperationSucceededDelegate =
            LoggerMessage.Define(
                LogLevel.Debug,
                default,
                "The operation completed successfully.");

        private static readonly Action<ILogger, Exception> LogOperationFailedDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "The operation failed.");

        private static readonly Action<ILogger, string, Exception> LogMetadataDoesNotExistDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                default,
                "The DICOM instance metadata file with '{DicomInstanceIdentifier}' does not exist.");

        private readonly IMetadataStore _metadataStore;
        private readonly ILogger _logger;

        public LoggingMetadataStore(IMetadataStore metadataStore, ILogger<LoggingMetadataStore> logger)
        {
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _metadataStore = metadataStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StoreInstanceMetadataAsync(DicomDataset dicomDataset, long version, CancellationToken cancellationToken)
        {
            LogStoreInstanceMetadataDelegate(_logger, dicomDataset.ToVersionedInstanceIdentifier(version).ToString(), null);

            try
            {
                await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, version, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteInstanceMetadataIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            LogDeleteInstanceMetadataDelegate(_logger, versionedInstanceIdentifier.ToString(), null);

            try
            {
                await _metadataStore.DeleteInstanceMetadataIfExistsAsync(versionedInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            string instanceIdentifierInString = versionedInstanceIdentifier.ToString();

            LogGetInstanceMetadataDelegate(_logger, instanceIdentifierInString, null);

            try
            {
                DicomDataset dicomDataset = await _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return dicomDataset;
            }
            catch (ItemNotFoundException ex)
            {
                LogMetadataDoesNotExistDelegate(_logger, instanceIdentifierInString, ex);

                throw;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        public async Task<Dictionary<int, FrameRange>> GetInstanceFramesRangeAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            return await _metadataStore.GetInstanceFramesRangeAsync(versionedInstanceIdentifier, cancellationToken);
        }

        public async Task StoreInstanceFramesRangeAsync(VersionedInstanceIdentifier identifier, Dictionary<int, FrameRange> framesRange, CancellationToken cancellationToken = default)
        {
            await _metadataStore.StoreInstanceFramesRangeAsync(identifier, framesRange, cancellationToken);
        }
    }
}
