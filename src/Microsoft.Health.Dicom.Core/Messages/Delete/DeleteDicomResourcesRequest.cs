// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using EnsureThat;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Delete
{
    public class DeleteDicomResourcesRequest : IRequest<DeleteDicomResourcesResponse>
    {
        public DeleteDicomResourcesRequest(Uri requestBaseUri, Stream requestBody, string requestContentType, string studyInstanceUID = null, string seriesUID = null, string instanceUID = null)
        {
            EnsureArg.IsNotNull(requestBaseUri, nameof(requestBaseUri));

            RequestContentType = requestContentType;
            RequestBaseUri = requestBaseUri;
            StudyInstanceUID = studyInstanceUID;
            SeriesUID = seriesUID;
            InstanceUID = instanceUID;
            IsBodyEmpty = requestBody.ReadByte() == -1;
        }

        public Uri RequestBaseUri { get; }

        public string StudyInstanceUID { get; }

        public string SeriesUID { get; }

        public string InstanceUID { get; }

        public string RequestContentType { get; }

        public bool IsBodyEmpty { get; }
    }
}
