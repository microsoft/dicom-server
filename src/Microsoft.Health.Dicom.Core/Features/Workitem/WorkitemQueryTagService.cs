// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemQueryTagService : IWorkitemQueryTagService, IDisposable
    {
        private readonly IIndexWorkitemStore _indexWorkitemStore;
        private readonly AsyncCache<IReadOnlyCollection<WorkitemQueryTagStoreEntry>> _queryTagCache;
        private bool _disposed;

        public WorkitemQueryTagService(IIndexWorkitemStore indexWorkitemStore)
        {
            _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
            _queryTagCache = new AsyncCache<IReadOnlyCollection<WorkitemQueryTagStoreEntry>>(ResolveQueryTagsAsync);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _queryTagCache.Dispose();
                }

                _disposed = true;
            }
        }

        public async Task<IReadOnlyCollection<WorkitemQueryTagStoreEntry>> GetQueryTagsAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WorkitemQueryTagService));
            }

            return await _queryTagCache.GetAsync(cancellationToken: cancellationToken);
        }

        private async Task<IReadOnlyCollection<WorkitemQueryTagStoreEntry>> ResolveQueryTagsAsync(CancellationToken cancellationToken)
        {
            return await _indexWorkitemStore.GetWorkitemQueryTagsAsync(cancellationToken);
        }
    }
}
