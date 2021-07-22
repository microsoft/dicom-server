// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.SqlServer.Exceptions;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    internal sealed class VersionedCache<T> : IDisposable where T : IVersioned
    {
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly Dictionary<SchemaVersion, T> _entities;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private volatile object _pendingInit;
        private T _value;

        public VersionedCache(ISchemaVersionResolver schemaVersionResolver, IEnumerable<T> versionedEntities)
        {
            _schemaVersionResolver = EnsureArg.IsNotNull(schemaVersionResolver, nameof(schemaVersionResolver));
            _entities = EnsureArg.IsNotNull(versionedEntities, nameof(versionedEntities))
                .Where(x => x != null)
                .ToDictionary(x => x.Version);

            // _pendingInit is a volatile reference type flag for determining whether init is needed.
            // It is arbitrarily assigned _semaphore instead of allocating a new object
            _pendingInit = _semaphore;
        }

        public void Dispose()
            => _semaphore.Dispose();

        public async Task<T> GetAsync(CancellationToken cancellationToken = default)
        {
            if (_pendingInit is not null)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
#pragma warning disable CA1508
                    // Another thread may have already gone through this block
                    if (_pendingInit is not null)
#pragma warning restore CA1508
                    {
                        SchemaVersion version = await _schemaVersionResolver.GetCurrentVersionAsync(cancellationToken);
                        if (!_entities.TryGetValue(version, out T value))
                        {
                            string msg = version == SchemaVersion.Unknown
                                ? DicomSqlServerResource.UnknownSchemaVersion
                                : string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.SchemaVersionOutOfRange, version);

                            throw new InvalidSchemaVersionException(msg);
                        }

                        _value = value;
                        _pendingInit = null; // Volatile write must occur after _value
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _value;
        }
    }
}
