// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public static class HttpRequestMessageExtensions
    {
        private const string DicomWebExceptionFactoryKey = "x-msh-exception-factory";

        public static void SetDicomWebExceptionFactory(this HttpRequestMessage request, DicomWebExceptionFactory factory)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            request.Properties[DicomWebExceptionFactoryKey] = factory;
        }

        public static DicomWebExceptionFactory GetDicomWebExceptionFactory(this HttpRequestMessage request)
        {
            if (request.Properties.TryGetValue(DicomWebExceptionFactoryKey, out object value))
            {
                return (DicomWebExceptionFactory)value;
            }

            return null;
        }
    }
}
