// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    /// <summary>
    /// Represents a Dicom server for end-to-end testing.
    /// </summary>
    public abstract class TestDicomWebServer : IDisposable
    {
        protected TestDicomWebServer(Uri baseAddress)
        {
            EnsureArg.IsNotNull(baseAddress, nameof(baseAddress));

            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }

        public DicomWebClient GetDicomWebClient(RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            return GetDicomWebClient(recyclableMemoryStreamManager, TestApplications.GlobalAdminServicePrincipal);
        }

        public DicomWebClient GetDicomWebClient(RecyclableMemoryStreamManager recyclableMemoryStreamManager, TestApplication clientApplication)
        {
            var httpClient = new HttpClient(new SessionMessageHandler(CreateMessageHandler())) { BaseAddress = BaseAddress };

            (bool enabled, string tokenUrl) securitySettings = (AuthenticationSettings.SecurityEnabled, AuthenticationSettings.TokenUrl);

            return new DicomWebClient(httpClient, this, recyclableMemoryStreamManager, clientApplication, securitySettings);
        }

        protected abstract HttpMessageHandler CreateMessageHandler();

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// An <see cref="HttpMessageHandler"/> that maintains session consistency between requests.
        /// </summary>
        private class SessionMessageHandler : DelegatingHandler
        {
            private string _sessionToken;

            public SessionMessageHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!string.IsNullOrEmpty(_sessionToken))
                {
                    request.Headers.TryAddWithoutValidation("x-ms-session-token", _sessionToken);
                }

                request.Headers.TryAddWithoutValidation("x-ms-consistency-level", "Session");

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Headers.TryGetValues("x-ms-session-token", out IEnumerable<string> tokens))
                {
                    _sessionToken = tokens.SingleOrDefault();
                }

                return response;
            }
        }
    }
}
