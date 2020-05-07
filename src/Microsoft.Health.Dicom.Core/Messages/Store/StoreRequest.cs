// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public class StoreRequest : IRequest<StoreResponse>
    {
        public StoreRequest(
            Stream requestBody,
            string requestContentType,
            string studyInstanceUid = null)
        {
            StudyInstanceUid = studyInstanceUid;
            RequestBody = requestBody;
            RequestContentType = requestContentType;
        }

        public string StudyInstanceUid { get; }

        public Stream RequestBody { get; }

        public string RequestContentType { get; }
    }
}
