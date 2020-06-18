// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;

namespace Microsoft.Health.Dicom.Client
{
    public class ManagedIdentityCredentialProvider : CredentialProvider
    {
        private readonly ManagedIdentityCredentialConfiguration _managedIdentityCredentialConfiguration;

        public ManagedIdentityCredentialProvider(ManagedIdentityCredentialConfiguration managedIdentityCredentialConfiguration)
        {
            EnsureArg.IsNotNull(managedIdentityCredentialConfiguration);

            _managedIdentityCredentialConfiguration = managedIdentityCredentialConfiguration;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync(_managedIdentityCredentialConfiguration.Resource, _managedIdentityCredentialConfiguration.Resource, cancellationToken);
        }
    }
}
