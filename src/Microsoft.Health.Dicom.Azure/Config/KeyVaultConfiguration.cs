// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Azure.Config;

public class KeyVaultConfiguration
{
    public const string SectionName = "KeyVault";

    public string Endpoint { get; set; }
}
