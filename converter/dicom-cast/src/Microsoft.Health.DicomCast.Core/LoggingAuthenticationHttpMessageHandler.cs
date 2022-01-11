// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Client
{
    public class LoggingAuthenticationHttpMessageHandler : DelegatingHandler
    {
        private readonly ICredentialProvider _credentialProvider;
        private readonly ILogger<LoggingAuthenticationHttpMessageHandler> _logger;

        public LoggingAuthenticationHttpMessageHandler(ICredentialProvider credentialProvider, ILogger<LoggingAuthenticationHttpMessageHandler> logger)
        {
            EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _credentialProvider = credentialProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            var bearer = await _credentialProvider.GetBearerToken(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("The token is: {Token}", bearer);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", bearer);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
