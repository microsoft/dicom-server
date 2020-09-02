// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Features.Security
{
    public class PrincipalClaimsExtractor : IClaimsExtractor
    {
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly SecurityConfiguration _securityConfiguration;

        public PrincipalClaimsExtractor(IDicomRequestContextAccessor dicomRequestContextAccessor, IOptions<SecurityConfiguration> securityConfiguration)
        {
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            EnsureArg.IsNotNull(securityConfiguration, nameof(securityConfiguration));

            _dicomRequestContextAccessor = dicomRequestContextAccessor;
            _securityConfiguration = securityConfiguration.Value;
        }

        public IReadOnlyCollection<KeyValuePair<string, string>> Extract()
        {
            return _dicomRequestContextAccessor.DicomRequestContext.Principal?.Claims?
                .Where(c => _securityConfiguration.PrincipalClaims?.Contains(c.Type) ?? false)
                .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
                .ToList();
        }
    }
}
