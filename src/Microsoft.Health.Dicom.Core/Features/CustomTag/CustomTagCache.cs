// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagCache : ICustomTagCache
    {
        private CustomTagList _customtags;
        private ICustomTagStore _customTagStore;
        private TimeSpan _expirationInterval;
        private DateTimeOffset? _lastRefreshTime;

        public CustomTagCache(ICustomTagStore customTagStore, TimeSpan expirationInterval)
        {
            EnsureArg.IsNotNull(customTagStore);
            _customtags = null;
            _lastRefreshTime = null;
            _customTagStore = customTagStore;
            _expirationInterval = expirationInterval;
        }

        public DateTimeOffset? LastRefreshTime { get => _lastRefreshTime; }

        public async Task<CustomTagList> GetCustomTagsAsync(CancellationToken cancellationToken = default)
        {
            if (IsOutDated())
            {
                CustomTagList customtags;
                bool refreshed = await _customTagStore.TryRefreshCustomTags(out customtags, cancellationToken);
                if (refreshed)
                {
                    _customtags = customtags;
                }

                UpdateLastRefreshTime();
            }

            return _customtags;
        }

        private bool IsInitialized()
        {
            return _customtags != null;
        }

        private bool IsOutDated()
        {
            return !IsInitialized() ||
                _lastRefreshTime.Value.AddMilliseconds(_expirationInterval.TotalMilliseconds) <= DateTimeOffset.UtcNow;
        }

        private void UpdateLastRefreshTime()
        {
            _lastRefreshTime = DateTimeOffset.UtcNow;
        }
    }
}
