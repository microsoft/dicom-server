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
using Microsoft.Health.Dicom.Core.Features.Store.Upload;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class DicomStoreHandler : IRequestHandler<DicomStoreRequest, DicomStoreResponse>
    {
        private readonly IEnumerable<IUploadedDicomInstanceReader> _uploadedDicomInstanceReaders;
        private readonly IDicomStoreService _dicomStoreService;
        private readonly ILogger _logger;

        public DicomStoreHandler(
            IEnumerable<IUploadedDicomInstanceReader> uploadedDicomInstanceReaders,
            IDicomStoreService dicomStoreService,
            ILogger<DicomStoreHandler> logger)
        {
            EnsureArg.IsNotNull(uploadedDicomInstanceReaders, nameof(uploadedDicomInstanceReaders));
            EnsureArg.IsNotNull(dicomStoreService, nameof(dicomStoreService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _uploadedDicomInstanceReaders = uploadedDicomInstanceReaders;
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
            IUploadedDicomInstanceReader uploadedDicomInstanceReader = _uploadedDicomInstanceReaders.FirstOrDefault(
                reader => reader.CanRead(message.RequestContentType));

            if (uploadedDicomInstanceReader == null)
            {
                _logger.LogWarning("The specified content type '{ContentType}' could not be processed.", message.RequestContentType);

                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, message.RequestContentType));
            }

            IReadOnlyCollection<IUploadedDicomInstance> uploadedDicomInstances = null;

            try
            {
                uploadedDicomInstances = await uploadedDicomInstanceReader.ReadAsync(
                    message.RequestContentType,
                    message.RequestBody,
                    cancellationToken);

                return await _dicomStoreService.ProcessUploadedDicomInstancesAsync(
                    message.StudyInstanceUid,
                    uploadedDicomInstances,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process uploaded DICOM instance(s).");
                throw;
            }
            finally
            {
                if (uploadedDicomInstances != null)
                {
                    _logger.LogTrace("Disposing all uploaded DICOM instances.");

                    IEnumerable<Task> disposeTasks = uploadedDicomInstances.Select(DisposeResourceAsync);

                    await Task.WhenAll(disposeTasks);
                }
            }
        }

        private async Task DisposeResourceAsync(IUploadedDicomInstance uploadedDicomInstance)
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
