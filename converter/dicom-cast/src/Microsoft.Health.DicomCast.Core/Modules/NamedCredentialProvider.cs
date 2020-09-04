// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Client;

namespace Microsoft.Health.DicomCast.Core.Modules
{
    public class NamedCredentialProvider : ICredentialProvider
    {
        private readonly ICredentialProvider _credentialProvider;

        public NamedCredentialProvider(string name, ICredentialProvider credentialProvider)
        {
            EnsureArg.IsNotNull(name, nameof(name));
            EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));

            Name = name;
            _credentialProvider = credentialProvider;
        }

        public string Name { get; }

        public Task<string> GetBearerToken(CancellationToken cancellationToken)
        {
            return _credentialProvider.GetBearerToken(cancellationToken);
        }
    }
}
