// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class DicomStoreHandler : IRequestHandler<DicomStoreRequest, DicomStoreResponse>
    {
        private readonly IEnumerable<IDicomInstanceEntryReader> _dicomInstanceEntryReaders;
        private readonly IDicomStoreService _dicomStoreService;
        private readonly ILogger _logger;

        public DicomStoreHandler(
            IEnumerable<IDicomInstanceEntryReader> dicomInstanceEntryReaders,
            IDicomStoreService dicomStoreService,
            ILogger<DicomStoreHandler> logger)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReaders, nameof(dicomInstanceEntryReaders));
            EnsureArg.IsNotNull(dicomStoreService, nameof(dicomStoreService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomInstanceEntryReaders = dicomInstanceEntryReaders;
            _dicomStoreService = dicomStoreService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<DicomStoreResponse> Handle(
            DicomStoreRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            DicomStoreRequestValidator.ValidateRequest(message);

            // Find a reader that can parse the request body.
            IDicomInstanceEntryReader dicomInstanceEntryReader = _dicomInstanceEntryReaders.FirstOrDefault(
                reader => reader.CanRead(message.RequestContentType));

            if (dicomInstanceEntryReader == null)
            {
                _logger.LogWarning("The specified content type '{ContentType}' could not be processed.", message.RequestContentType);

                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, message.RequestContentType));
            }

            IReadOnlyCollection<IDicomInstanceEntry> dicomInstanceEntries = null;

            try
            {
                dicomInstanceEntries = await dicomInstanceEntryReader.ReadAsync(
                    message.RequestContentType,
                    message.RequestBody,
                    cancellationToken);

                return await _dicomStoreService.ProcessDicomInstanceEntriesAsync(
                    message.StudyInstanceUid,
                    dicomInstanceEntries,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process uploaded DICOM instance(s).");
                throw;
            }
            finally
            {
                if (dicomInstanceEntries != null)
                {
                    _logger.LogTrace("Disposing all uploaded DICOM instances.");

                    IEnumerable<Task> disposeTasks = dicomInstanceEntries.Select(DisposeResourceAsync);

                    await Task.WhenAll(disposeTasks);
                }
            }
        }

        private async Task DisposeResourceAsync(IDicomInstanceEntry uploadedDicomInstance)
        {
            try
            {
                await uploadedDicomInstance.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose the uploaded DICOM instance.");
            }
        }
    }
}
