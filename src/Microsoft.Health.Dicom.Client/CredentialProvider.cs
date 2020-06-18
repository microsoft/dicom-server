// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Health.Dicom.Client
{
    public abstract class CredentialProvider : ICredentialProvider
    {
        private DateTime _tokenExpiration;
        private string _token;
        private readonly TimeSpan _tokenTimeout = TimeSpan.FromMinutes(5);

        public async Task<string> GetBearerToken(CancellationToken cancellationToken)
        {
            if (_tokenExpiration < DateTime.UtcNow + _tokenTimeout)
            {
                _token = await BearerTokenFunction(cancellationToken);
                var decodedToken = new JsonWebToken(_token);
                _tokenExpiration = decodedToken.ValidTo;
            }

            return _token;
        }

        protected abstract Task<string> BearerTokenFunction(CancellationToken cancellationToken);
    }
}
