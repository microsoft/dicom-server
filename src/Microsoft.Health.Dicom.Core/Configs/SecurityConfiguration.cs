// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Security;

namespace Microsoft.Health.Dicom.Core.Configs;

public class SecurityConfiguration
{
    public bool Enabled { get; set; }

    public IEnumerable<AuthenticationConfiguration> AuthenticationSchemes { get; set; } = new List<AuthenticationConfiguration>();

    public virtual HashSet<string> PrincipalClaims { get; } = new HashSet<string>(StringComparer.Ordinal);

    public AuthorizationConfiguration<DataActions> Authorization { get; set; } = new AuthorizationConfiguration<DataActions>();
}
