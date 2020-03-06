// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Delete;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Delete
{
    public class DeleteDicomResourcesHandler : IRequestHandler<DeleteDicomResourcesRequest, DeleteDicomResourcesResponse>
    {
        private readonly IDicomDataStore _dicomDataStore;

        public DeleteDicomResourcesHandler(IDicomDataStore dicomDataStore)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));

            _dicomDataStore = dicomDataStore;
        }

        /// <inheritdoc />
        public async Task<DeleteDicomResourcesResponse> Handle(DeleteDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            try
            {
                switch (message.ResourceType)
                {
                    case ResourceType.Study:
                        await _dicomDataStore.DeleteStudyAsync(message.StudyInstanceUID, cancellationToken);
                        break;
                    case ResourceType.Series:
                        await _dicomDataStore.DeleteSeriesAsync(message.StudyInstanceUID, message.SeriesInstanceUID, cancellationToken);
                        break;
                    case ResourceType.Instance:
                        await _dicomDataStore.DeleteInstanceAsync(message.StudyInstanceUID, message.SeriesInstanceUID, message.SopInstanceUID, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"Unkown delete transaction type: {message.ResourceType}", nameof(message));
                }
            }
            catch (DataStoreException e)
            {
                return new DeleteDicomResourcesResponse(e.StatusCode);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.OK);
        }
    }
}
