// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Health.Dicom.Core.Features.Security;

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class AuthorizationConfiguration
    {
        public string RolesClaim { get; set; } = "roles";

        public bool Enabled { get; set; }

        public IReadOnlyList<Role> Roles { get; internal set; } = ImmutableList<Role>.Empty;
    }
}
