// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveResourceResponse
    {
        public RetrieveResourceResponse(IEnumerable<Stream> responseStreams)
        {
            EnsureArg.IsNotNull(responseStreams, nameof(responseStreams));
            ResponseStreams = responseStreams;
        }

        public IEnumerable<Stream> ResponseStreams { get; }
    }
}
