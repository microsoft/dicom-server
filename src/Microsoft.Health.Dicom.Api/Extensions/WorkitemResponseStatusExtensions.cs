// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class WorkitemResponseStatusExtensions
    {
        private static readonly IReadOnlyDictionary<WorkitemResponseStatus, HttpStatusCode> AddResponseStatusToHttpStatusCodeMapping =
            new Dictionary<WorkitemResponseStatus, HttpStatusCode>()
            {
                { WorkitemResponseStatus.None, HttpStatusCode.NoContent },
                { WorkitemResponseStatus.Success, HttpStatusCode.Created },
                { WorkitemResponseStatus.Failure, HttpStatusCode.BadRequest },
                { WorkitemResponseStatus.Conflict, HttpStatusCode.Conflict },
            };

        private static readonly IReadOnlyDictionary<WorkitemResponseStatus, HttpStatusCode> CancelResponseStatusToHttpStatusCodeMapping =
            new Dictionary<WorkitemResponseStatus, HttpStatusCode>()
            {
                { WorkitemResponseStatus.None, HttpStatusCode.NotFound },
                { WorkitemResponseStatus.Success, HttpStatusCode.Accepted },
                { WorkitemResponseStatus.Failure, HttpStatusCode.BadRequest },
                { WorkitemResponseStatus.Conflict, HttpStatusCode.Conflict }
            };

        /// <summary>
        /// Converts from <see cref="WorkitemResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode AddResponseToHttpStatusCode(this WorkitemResponseStatus status)
            => AddResponseStatusToHttpStatusCodeMapping[status];

        /// <summary>
        /// Converts from <see cref="WorkitemResponseStatus"/> to <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="status">The status to convert.</param>
        /// <returns>The converted <see cref="HttpStatusCode"/>.</returns>
        public static HttpStatusCode CancelResponseToHttpStatusCode(this WorkitemResponseStatus status)
            => CancelResponseStatusToHttpStatusCodeMapping[status];
    }
}
