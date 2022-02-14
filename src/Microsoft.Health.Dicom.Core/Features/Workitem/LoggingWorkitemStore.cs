// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class LoggingWorkitemStore : IWorkitemStore
    {
        private readonly IWorkitemStore _workitemStore;
        private readonly ILogger _logger;

        public LoggingWorkitemStore(IWorkitemStore workitemStore, ILogger<LoggingWorkitemStore> logger)
        {
            EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _workitemStore = workitemStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(WorkitemInstanceIdentifier identifier, DicomDataset dataset, long? proposedWatermark = default, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            _logger.LogDebug("Adding workitem '{WorkitemInstanceIdentifier}'.", identifier);

            try
            {
                await _workitemStore.AddWorkitemAsync(identifier, dataset, proposedWatermark, cancellationToken);

                _logger.LogDebug("The operation completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The operation failed.");

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetWorkitemAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            _logger.LogDebug("Querying workitem '{WorkitemInstanceIdentifier}'.", identifier);

            try
            {
                var result = await _workitemStore.GetWorkitemAsync(identifier, cancellationToken);

                _logger.LogDebug("The operation completed successfully.");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The operation failed.");

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteWorkitemAsync(WorkitemInstanceIdentifier identifier, long? proposedWatermark = default, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            _logger.LogDebug("Querying workitem '{WorkitemInstanceIdentifier}'.", identifier);

            try
            {
                await _workitemStore.DeleteWorkitemAsync(identifier, proposedWatermark, cancellationToken);

                _logger.LogDebug("The operation completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The operation failed.");

                throw;
            }
        }
    }
}
