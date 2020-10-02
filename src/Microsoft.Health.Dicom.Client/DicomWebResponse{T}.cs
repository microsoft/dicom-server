// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebResponse<T> : DicomWebResponse
    {
        private Func<HttpContent, CancellationToken, Task<T>> _valueFactory;

        public DicomWebResponse(HttpResponseMessage response, T value)
            : base(response)
        {
            Value = value;
        }

        public DicomWebResponse(HttpResponseMessage response, Func<HttpContent, CancellationToken, Task<T>> valueFactory)
            : base(response)
        {
            _valueFactory = valueFactory;
        }

        public T Value { get; }

        public Task<T> GetValueAsync(CancellationToken cancellationToken)
            => _valueFactory(Content, cancellationToken);

    }
}
