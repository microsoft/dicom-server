// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveResourceResponse
    {
        public RetrieveResourceResponse(IEnumerable<RetrieveResourceInstance> responseStreams, string contentType)
        {
            ResponseInstances = EnsureArg.IsNotNull(responseStreams, nameof(responseStreams)); ;
            ContentType = EnsureArg.IsNotEmptyOrWhiteSpace(contentType, nameof(contentType)); ;
        }

        /// <summary>
        /// Collection of instance streams and properties used in response
        /// </summary>
        public IEnumerable<RetrieveResourceInstance> ResponseInstances { get; }

        public string ContentType { get; }
    }
}
