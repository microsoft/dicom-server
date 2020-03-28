// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public class StoreDicomResourcesRequest : IRequest<StoreDicomResourcesResponse>
    {
        public StoreDicomResourcesRequest(
            Uri requestBaseUri,
            Stream requestBody,
            string requestContentType,
            string studyInstanceUID = null)
        {
            StudyInstanceUid = studyInstanceUID;
            RequestBaseUri = requestBaseUri;
            RequestBody = requestBody;
            RequestContentType = requestContentType;
        }

        public string StudyInstanceUid { get; }

        public Uri RequestBaseUri { get; }

        public Stream RequestBody { get; }

        public string RequestContentType { get; }
    }
}
