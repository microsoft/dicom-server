// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Health.Dicom.Azure;

internal sealed class KeyVaultSecretClientOptions : SecretClientOptions
{
    public const string SectionName = "KeyVault";

    public bool Enabled { get; set; }
}
