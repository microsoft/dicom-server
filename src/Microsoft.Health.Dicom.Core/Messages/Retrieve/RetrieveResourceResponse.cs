// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveResourceResponse
    {
        public RetrieveResourceResponse(IAsyncEnumerable<RetrieveResourceInstance> responseStreams, string contentType, bool isSinglePart = false)
        {
            ResponseInstances = EnsureArg.IsNotNull(responseStreams, nameof(responseStreams));
            ContentType = EnsureArg.IsNotEmptyOrWhiteSpace(contentType, nameof(contentType));
            IsSinglePart = isSinglePart;
        }

        /// <summary>
        /// Collection of instance streams and properties used in response
        /// </summary>
        public IAsyncEnumerable<RetrieveResourceInstance> ResponseInstances { get; }

        public string ContentType { get; }

        public bool IsSinglePart { get; }

        public IAsyncEnumerator<RetrieveResourceInstance> GetResponseInstancesEnumerator(CancellationToken cancellationToken)
        {
            return ResponseInstances.GetAsyncEnumerator(cancellationToken);
        }
    }
}
