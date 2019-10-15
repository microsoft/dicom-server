// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class SecurityConfiguration
    {
        public bool Enabled { get; set; }

        public AuthenticationConfiguration Authentication { get; set; } = new AuthenticationConfiguration();
    }
}
