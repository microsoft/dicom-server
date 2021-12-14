// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.Health.Dicom.Core.Messages.WorkItemMessages;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class WorkItemStoreResponseStatusExtensions
    {
        private static readonly IReadOnlyDictionary<WorkItemStoreResponseStatus, HttpStatusCode> StoreResponseStatusToHttpStatusCodeMapping =
            new Dictionary<WorkItemStoreResponseStatus, HttpStatusCode>()
            {
                { WorkItemStoreResponseStatus.None, HttpStatusCode.NoContent },
                { WorkItemStoreResponseStatus.Success, HttpStatusCode.OK },
                { WorkItemStoreResponseStatus.Failure, HttpStatusCode.Conflict },
            };

        /// <summary>
        /// Converts from <see cref="WorkItemStoreResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode ToHttpStatusCode(this WorkItemStoreResponseStatus status)
            => StoreResponseStatusToHttpStatusCodeMapping[status];
    }
}
