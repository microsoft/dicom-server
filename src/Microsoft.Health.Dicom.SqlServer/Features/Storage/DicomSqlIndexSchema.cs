// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage
{
    public sealed class DicomSqlIndexSchema : IDisposable
    {
        private readonly SchemaInformation _schemaInformation;
        private readonly ILogger<DicomSqlIndexSchema> _logger;

        private readonly RetryableInitializationOperation _initializationOperation;

        public DicomSqlIndexSchema(
            SchemaInformation schemaInformation,
            ILogger<DicomSqlIndexSchema> logger)
        {
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _schemaInformation = schemaInformation;
            _logger = logger;

            _initializationOperation = new RetryableInitializationOperation(InitializeAsync);

            if (schemaInformation.Current != null)
            {
                // kick off initialization so that it can be ready for requests. Errors will be observed by requests when they call the method.
                EnsureInitialized();
            }
        }

        public void Dispose()
        {
            _initializationOperation.Dispose();
        }

        public ValueTask EnsureInitialized() => _initializationOperation.EnsureInitialized();

        private Task InitializeAsync()
        {
            if (!_schemaInformation.Current.HasValue)
            {
                _logger.LogError($"The current version of the database is not available. Unable in initialize {nameof(DicomSqlIndexDataStore)}.");
                throw new ServiceUnavailableException();
            }

            return Task.CompletedTask;
        }
    }
}
