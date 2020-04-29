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
    /// Provides extension methods for <see cref="DicomStoreResponseStatus"/>.
    /// </summary>
    public static class DicomStoreResponseStatusExtensions
    {
        private static readonly IReadOnlyDictionary<DicomStoreResponseStatus, HttpStatusCode> StoreResponseStatusToHttpStatusCodeMapping = new Dictionary<DicomStoreResponseStatus, HttpStatusCode>()
        {
            { DicomStoreResponseStatus.None, HttpStatusCode.NoContent },
            { DicomStoreResponseStatus.Success, HttpStatusCode.OK },
            { DicomStoreResponseStatus.PartialSuccess, HttpStatusCode.Accepted },
            { DicomStoreResponseStatus.Failure, HttpStatusCode.Conflict },
        };

        /// <summary>
        /// Converts from <see cref="DicomStoreResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode ToHttpStatusCode(this DicomStoreResponseStatus status)
            => StoreResponseStatusToHttpStatusCodeMapping[status];
    }
}
