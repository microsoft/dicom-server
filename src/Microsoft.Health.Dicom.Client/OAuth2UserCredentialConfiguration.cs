// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client
{
    public class OAuth2UserCredentialConfiguration : OAuth2ClientCredentialConfiguration
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
