// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Client;

namespace Microsoft.Health.DicomCast.Core.Configurations
{
    public class AuthenticationConfiguration
    {
        public bool Enabled { get; set; }

        public AuthenticationType? AuthenticationType { get; set; }

        public OAuth2ClientCredentialConfiguration OAuth2ClientCredential { get; set; }

        public OAuth2UserPasswordCredentialConfiguration OAuth2UserPasswordCredential { get; set; }

        public ManagedIdentityCredentialConfiguration ManagedIdentityCredential { get; set; }
    }
}
