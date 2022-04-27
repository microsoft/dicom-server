// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common;

public interface ISecretStore
{
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
}
