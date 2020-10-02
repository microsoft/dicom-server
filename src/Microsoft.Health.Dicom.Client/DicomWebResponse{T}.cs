// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebResponse<T> : DicomWebResponse
    {
        public DicomWebResponse(HttpResponseMessage response, T value)
            : base(response)
        {
            Value = value;
        }

        public T Value { get; }

        public static implicit operator T(DicomWebResponse<T> response)
        {
            EnsureArg.IsNotNull(response, nameof(response));

            return response.Value;
        }

        public T ToT()
        {
            return Value;
        }
    }
}
