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
    internal sealed class VersionedCache<T> : IDisposable where T : class, IVersioned
    {
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly Dictionary<SchemaVersion, T> _entities;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private volatile T _cachedEntity;

        public VersionedCache(ISchemaVersionResolver schemaVersionResolver, IEnumerable<T> versionedEntities)
        {
            _schemaVersionResolver = EnsureArg.IsNotNull(schemaVersionResolver, nameof(schemaVersionResolver));
            _entities = EnsureArg.IsNotNull(versionedEntities, nameof(versionedEntities))
                .Where(x => x != null)
                .ToDictionary(x => x.Version);
        }

        public void Dispose()
            => _semaphore.Dispose();

        public async Task<T> GetAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedEntity is null)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
#pragma warning disable CA1508
                    // Cached entity may be set by another thread
                    if (_cachedEntity is null)
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

                        _cachedEntity = value;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _cachedEntity;
        }
    }
}
