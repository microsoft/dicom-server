// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="StoreResponseStatus"/>.
    /// </summary>
    public static class StoreResponseStatusExtensions
    {
        private static readonly IReadOnlyDictionary<StoreResponseStatus, HttpStatusCode> StoreResponseStatusToHttpStatusCodeMapping = new Dictionary<StoreResponseStatus, HttpStatusCode>()
        {
            { StoreResponseStatus.None, HttpStatusCode.NoContent },
            { StoreResponseStatus.Success, HttpStatusCode.OK },
            { StoreResponseStatus.PartialSuccess, HttpStatusCode.Accepted },
            { StoreResponseStatus.Failure, HttpStatusCode.Conflict },
        };

        /// <summary>
        /// Converts from <see cref="StoreResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode ToHttpStatusCode(this StoreResponseStatus status)
            => StoreResponseStatusToHttpStatusCodeMapping[status];
    }
}
