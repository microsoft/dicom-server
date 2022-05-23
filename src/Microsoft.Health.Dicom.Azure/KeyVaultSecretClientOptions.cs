// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Health.Dicom.Azure;

internal sealed class KeyVaultSecretClientOptions : SecretClientOptions
{
    public const string SectionName = "KeyVault";

    // This value is based on the SecretClient's parameter name and is read automatically from the configuration
    public Uri VaultUri { get; set; }

    [Obsolete]
    public Uri Endpoint
    {
        get => _endpoint;
        set
        {
            _endpoint = value;
            VaultUri = value;
        }
    }

    private Uri _endpoint;
}
