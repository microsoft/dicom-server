// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebResponseHandler : DelegatingHandler
    {
        private static readonly DefaultDicomWebExceptionFactory DefaultDicomWebExceptionFactory = (statusCode, responseHeaders, contentHeaders, responseBody) =>
            new DicomWebException(
                statusCode,
                responseHeaders,
                contentHeaders,
                responseBody);

        public DicomWebResponseHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response).ConfigureAwait(false);

            return response;
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                DicomWebException exception = null;

                try
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    DicomWebExceptionFactory factory = response.RequestMessage.GetDicomWebExceptionFactory();

                    if (factory == null)
                    {
                        exception = DefaultDicomWebExceptionFactory(
                            response.StatusCode,
                            response.Headers,
                            response.Content?.Headers,
                            responseBody);
                    }
                    else
                    {
                        exception = factory.Invoke(
                            DefaultDicomWebExceptionFactory,
                            response.StatusCode,
                            response.Headers,
                            response.Content?.Headers,
                            responseBody);
                    }
                }
                finally
                {
                    // If we are throwing exception, then we can close the response because we have already read the body.
                    if (exception != null)
                    {
                        response.Dispose();
                    }
                }

                if (exception != null)
                {
                    throw exception;
                }
            }
        }
    }
}
