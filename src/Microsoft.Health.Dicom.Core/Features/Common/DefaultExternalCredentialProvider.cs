// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Microsoft.Health.Core.Features.Identity;

namespace Microsoft.Health.Dicom.Core.Features.Common;

internal sealed class DefaultExternalCredentialProvider : IExternalCredentialProvider
{
    // TODO: Allow users to configure defaults
    public TokenCredential GetTokenCredential()
        => new DefaultAzureCredential(includeInteractiveCredentials: false);
}
