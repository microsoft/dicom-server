// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveDicomResourceRequest : IRequest<RetrieveDicomResourceResponse>
    {
        /// <summary>
        /// If the requested transfer syntax equals '*', the caller is requesting the original transfer syntax of the uploaded file.
        /// </summary>
        private const string OriginalTransferSyntaxRequest = "*";

        public RetrieveDicomResourceRequest(IDicomResource dicomResource, string requestedTransferSyntax = null)
        {
            DicomResource = dicomResource;
            RequestedTransferSyntax =
                requestedTransferSyntax.Equals(OriginalTransferSyntaxRequest, StringComparison.InvariantCultureIgnoreCase) ?
                    null : requestedTransferSyntax;
        }

        public IDicomResource DicomResource { get; }

        public string RequestedTransferSyntax { get; }
    }
}
