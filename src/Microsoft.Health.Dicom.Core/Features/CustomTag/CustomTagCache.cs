// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagCache : ICustomTagCache
    {
        private readonly ICustomTagStore _customTagStore;

        private IReadOnlyCollection<CustomTagEntry> _cache;

        public CustomTagCache(ICustomTagStore customTagStore)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            _customTagStore = customTagStore;
        }

        public async Task<IReadOnlyCollection<CustomTagEntry>> GetCustomTagsAsync(CancellationToken cancellationToken)
        {
            if (_cache == null)
            {
                _cache = await _customTagStore.GetCustomTagsAsync(path: null, cancellationToken: cancellationToken);
            }

            return _cache;
        }
    }
}
