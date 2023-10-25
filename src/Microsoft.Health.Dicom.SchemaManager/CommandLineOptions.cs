// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.Dicom.SchemaManager;

public class CommandLineOptions
{
    public Uri? Server { get; set; }

    public string? ConnectionString { get; set; }

    [Obsolete("Use Connection String instead for different authentication types.")]
    public SqlServerAuthenticationType? AuthenticationType { get; set; }

    [Obsolete("Use Connection String instead for different authentication types.")]
    public string? ManagedIdentityClientId { get; set; }

    public bool EnableWorkloadIdentity { get; set; }
}
