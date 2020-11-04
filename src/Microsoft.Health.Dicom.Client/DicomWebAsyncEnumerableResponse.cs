// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebAsyncEnumerableResponse<T> : DicomWebResponse, IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _enumerable;

        public DicomWebAsyncEnumerableResponse(HttpResponseMessage response, IAsyncEnumerable<T> enumerable)
            : base(response)
        {
            _enumerable = enumerable;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => _enumerable.GetAsyncEnumerator(cancellationToken);
    }
}
