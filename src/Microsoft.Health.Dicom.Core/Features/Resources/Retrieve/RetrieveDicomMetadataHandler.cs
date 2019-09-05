// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    internal class RetrieveDicomMetadataHandler : BaseRetrieveDicomResourceHandler, IRequestHandler<RetrieveDicomMetadataRequest, RetrieveDicomMetadataResponse>
    {
        private readonly IDicomInstanceMetadataStore _dicomInstanceMetadataStore;

        public RetrieveDicomMetadataHandler(IDicomMetadataStore dicomMetadataStore, IDicomInstanceMetadataStore dicomInstanceMetadataStore)
            : base(dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));

            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
        }

        public async Task<RetrieveDicomMetadataResponse> Handle(RetrieveDicomMetadataRequest message, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<DicomInstance> retrieveInstances = await GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUID, message.SeriesInstanceUID, message.SopInstanceUID, cancellationToken);
                IEnumerable<DicomDataset> responseMetadata = await Task.WhenAll(
                    retrieveInstances.Select(x => _dicomInstanceMetadataStore.GetInstanceMetadataAsync(x, cancellationToken)));

                return new RetrieveDicomMetadataResponse(HttpStatusCode.OK, responseMetadata.ToArray());
            }
            catch (DataStoreException e)
            {
                return new RetrieveDicomMetadataResponse(e.StatusCode);
            }
        }
    }
}
