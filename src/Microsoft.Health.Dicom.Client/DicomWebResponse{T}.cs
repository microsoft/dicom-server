// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebResponse<T> : DicomWebResponse
    {
        private readonly Func<HttpContent, Task<T>> _valueFactory;

        public DicomWebResponse(HttpResponseMessage response, Func<HttpContent, Task<T>> valueFactory)
            : base(response)
        {
            EnsureArg.IsNotNull(valueFactory, nameof(valueFactory));

            _valueFactory = valueFactory;
        }

        public Task<T> GetValueAsync()
            => _valueFactory(Content);
    }
}
