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
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    internal class RetrieveDicomMetadataHandler : IRequestHandler<RetrieveDicomMetadataRequest, RetrieveDicomMetadataResponse>
    {
        private readonly IDicomRetrieveMetadataService _dicomRetrieveMetadataService;

        public RetrieveDicomMetadataHandler(IDicomRetrieveMetadataService dicomRetrieveMetadataService)
        {
            EnsureArg.IsNotNull(dicomRetrieveMetadataService, nameof(dicomRetrieveMetadataService));
            _dicomRetrieveMetadataService = dicomRetrieveMetadataService;
        }

        public async Task<RetrieveDicomMetadataResponse> Handle(RetrieveDicomMetadataRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomDataset> responseMetadata = await _dicomRetrieveMetadataService.GetDicomInstanceMetadataAsync(
                message.ResourceType,
                message.StudyInstanceUid,
                message.SeriesInstanceUid,
                message.SopInstanceUid,
                cancellationToken);

            return new RetrieveDicomMetadataResponse(HttpStatusCode.OK, responseMetadata.ToArray());
        }
    }
}
