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
using Microsoft.Health.Dicom.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Exceptions;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    internal sealed class VersionedCache<T> : IDisposable where T : IVersioned
    {
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly IEnumerable<T> _entities;
        private readonly AsyncCache<T> _cache;

        public VersionedCache(ISchemaVersionResolver schemaVersionResolver, IEnumerable<T> versionedEntities)
        {
            _schemaVersionResolver = EnsureArg.IsNotNull(schemaVersionResolver, nameof(schemaVersionResolver));
            _entities = EnsureArg.IsNotNull(versionedEntities, nameof(versionedEntities))
                .Where(x => x != null)
                .OrderByDescending(x => x.Version);
            _cache = new AsyncCache<T>(ResolveAsync);
        }

        public void Dispose()
            => _cache.Dispose();

        public Task<T> GetAsync(CancellationToken cancellationToken = default)
            => _cache.GetAsync(forceRefresh: false, cancellationToken: cancellationToken); // prevent users from forcing refresh

        private async Task<T> ResolveAsync(CancellationToken cancellationToken = default)
        {
            SchemaVersion version = await _schemaVersionResolver.GetCurrentVersionAsync(cancellationToken);

            // Return the latest version of the entity. If the entity is not found, throw an exception.
            T value = _entities.Where(entity => entity.Version <= version && (int)entity.Version >= SchemaVersionConstants.Min).FirstOrDefault();

            if (value != null) return value;

            string msg = version == SchemaVersion.Unknown
                    ? DicomSqlServerResource.UnknownSchemaVersion
                    : string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.SchemaVersionOutOfRange, version);

            throw new InvalidSchemaVersionException(msg);
        }
    }
}
