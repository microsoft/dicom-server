// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Configs;

namespace Microsoft.Health.Dicom.Core.Configs;

public class CoreConfiguration
{
    public AuditConfiguration Audit { get; } = new AuditConfiguration("X-MS-AZUREDICOM-AUDIT-");

    public FeatureConfiguration Features { get; } = new FeatureConfiguration();

    public SecurityConfiguration Security { get; } = new SecurityConfiguration();

    public ServicesConfiguration Services { get; } = new ServicesConfiguration();
}
