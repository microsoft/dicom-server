// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public class LoggingMetadataStore : IMetadataStore
    {
        private static readonly Action<ILogger, string, Exception> LogAddInstanceMetadataDelegate =
               LoggerMessage.Define<string>(
                   LogLevel.Debug,
                   default,
                   "Adding DICOM instance metadata file with '{DicomInstanceIdentifier}'.");

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
        public async Task AddInstanceMetadataAsync(DicomDataset dicomDataset, long version, CancellationToken cancellationToken)
        {
            LogAddInstanceMetadataDelegate(_logger, dicomDataset.ToVersionedInstanceIdentifier(version).ToString(), null);

            try
            {
                await _metadataStore.AddInstanceMetadataAsync(dicomDataset, version, cancellationToken);

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
            LogGetInstanceMetadataDelegate(_logger, versionedInstanceIdentifier.ToString(), null);

            try
            {
                DicomDataset dicomDataset = await _metadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return dicomDataset;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }
    }
}
