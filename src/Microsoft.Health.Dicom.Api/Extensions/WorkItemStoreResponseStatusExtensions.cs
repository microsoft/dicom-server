// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class WorkitemStoreResponseStatusExtensions
    {
        private static readonly IReadOnlyDictionary<WorkitemStoreResponseStatus, HttpStatusCode> StoreResponseStatusToHttpStatusCodeMapping =
            new Dictionary<WorkitemStoreResponseStatus, HttpStatusCode>()
            {
                { WorkitemStoreResponseStatus.None, HttpStatusCode.NoContent },
                { WorkitemStoreResponseStatus.Success, HttpStatusCode.OK },
                { WorkitemStoreResponseStatus.Failure, HttpStatusCode.Conflict },
            };

        /// <summary>
        /// Converts from <see cref="WorkitemStoreResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode ToHttpStatusCode(this WorkitemStoreResponseStatus status)
            => StoreResponseStatusToHttpStatusCodeMapping[status];
    }
}
