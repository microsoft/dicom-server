// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Duplicate;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Functions.Duplicate;

/// <summary>
/// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
/// based on new tags configured by the user.
/// </summary>
public partial class DuplicateDurableFunction
{
    private readonly IInstanceStore _instanceStore;
    private readonly DuplicationOptions _options;
    private readonly IInstanceDuplicater _instanceDuplicater;

    public DuplicateDurableFunction(
        IInstanceStore instanceStore,
        IInstanceDuplicater instanceDuplicater,
        IOptions<DuplicationOptions> configOptions)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _instanceDuplicater = EnsureArg.IsNotNull(instanceDuplicater, nameof(instanceDuplicater));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
