// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Operations;

internal sealed class DicomOperationsResourceStore : IDicomOperationsResourceStore
{
    private readonly IExtendedQueryTagStore _queryTagStore;

    public DicomOperationsResourceStore(IExtendedQueryTagStore queryTagStore)
        => _queryTagStore = EnsureArg.IsNotNull(queryTagStore, nameof(queryTagStore));

    public async IAsyncEnumerable<string> ResolveQueryTagKeysAsync(IReadOnlyCollection<int> keys, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(keys, nameof(keys));
        EnsureArg.HasItems(keys, nameof(keys));

        IReadOnlyList<ExtendedQueryTagStoreJoinEntry> entries = await _queryTagStore.GetExtendedQueryTagsAsync(keys, cancellationToken);
        foreach (string path in entries.Select(x => x.Path))
        {
            yield return path;
        }
    }
}
