// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public class OAuth2ClientCredentialConfiguration
    {
        public OAuth2ClientCredentialConfiguration()
        {
        }

        public OAuth2ClientCredentialConfiguration(Uri tokenUri, string resource, string scope, string clientId, string clientSecret)
        {
            EnsureArg.IsNotNull(tokenUri, nameof(tokenUri));
            EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));
            EnsureArg.IsNotNullOrWhiteSpace(scope, nameof(scope));
            EnsureArg.IsNotNullOrWhiteSpace(clientId, nameof(clientId));
            EnsureArg.IsNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));

            TokenUri = tokenUri;
            Resource = resource;
            Scope = scope;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public Uri TokenUri { get; set; }

        public string Resource { get; set; }

        public string Scope { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
