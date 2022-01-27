// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class WorkitemResponseStatusExtensions
    {
        private static readonly IReadOnlyDictionary<WorkitemResponseStatus, HttpStatusCode> ResponseStatusToHttpStatusCodeMapping =
            new Dictionary<WorkitemResponseStatus, HttpStatusCode>()
            {
                { WorkitemResponseStatus.None, HttpStatusCode.NoContent },
                { WorkitemResponseStatus.Success, HttpStatusCode.OK },
                { WorkitemResponseStatus.Failure, HttpStatusCode.Conflict },
            };

        /// <summary>
        /// Converts from <see cref="WorkitemResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode ToHttpStatusCode(this WorkitemResponseStatus status)
            => ResponseStatusToHttpStatusCodeMapping[status];
    }
}
