// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveDicomResourceResponse : BaseStatusCodeResponse
    {
        public RetrieveDicomResourceResponse(int statusCode)
            : base(statusCode)
        {
        }

        public RetrieveDicomResourceResponse(HttpStatusCode statusCode, IEnumerable<Stream> responseStreams)
            : base((int)statusCode)
        {
            EnsureArg.IsNotNull(responseStreams, nameof(responseStreams));
            ResponseStreams = responseStreams;
        }

        public IEnumerable<Stream> ResponseStreams { get; }
    }
}
