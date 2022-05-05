// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Copy;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Functions.Copy;

/// <summary>
/// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
/// based on new tags configured by the user.
/// </summary>
public partial class CopyDurableFunction
{
    private readonly IInstanceStore _instanceStore;
    private readonly CopyOptions _options;
    private readonly IInstanceCopier _instanceCopier;

    public CopyDurableFunction(
        IInstanceStore instanceStore,
        IInstanceCopier instanceCopier,
        IOptions<CopyOptions> configOptions)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _instanceCopier = EnsureArg.IsNotNull(instanceCopier, nameof(instanceCopier));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
