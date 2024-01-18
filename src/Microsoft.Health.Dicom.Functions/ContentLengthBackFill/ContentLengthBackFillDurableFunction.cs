// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Functions.ContentLengthBackFill;

public partial class ContentLengthBackFillDurableFunction
{
    private readonly IInstanceStore _instanceStore;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IFileStore _fileStore;
    private readonly ContentLengthBackFillOptions _options;

    public ContentLengthBackFillDurableFunction(
        IInstanceStore instanceStore,
        IIndexDataStore indexDataStore,
        IFileStore fileStore,
        IOptions<ContentLengthBackFillOptions> configOptions)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
