// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal class Proxied<T>
    {
        /// <summary>
        /// Gets the optional instance ID for the proxy's upstream orchestration.
        /// </summary>
        /// <remarks>
        /// Depending on the proxy implementation, the ID may not ultimately be used.
        /// </remarks>
        public string UpstreamInstanceId { get; init; }

        public T Value { get; init; }
    }
}
