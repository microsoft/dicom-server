// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Feature.Common;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    /// <summary>
    ///  Sql implementation of IStoreFactory, which takes multiple versioned stores, and return the one that should be used.
    /// </summary>
    /// <typeparam name="TVersionedStore">Type of versioned store.</typeparam>
    /// <typeparam name="TStore">Type of the store.</typeparam>
    internal class SqlStoreFactory<TVersionedStore, TStore> : IStoreFactory<TStore>
        where TVersionedStore : IVersioned, TStore
    {
        private readonly ISchemaVersionResolver _schemaResolver;
        private readonly Dictionary<SchemaVersion, TVersionedStore> _versionedStores;

        public SqlStoreFactory(ISchemaVersionResolver schemaResolver, IEnumerable<TVersionedStore> versionedStores)
        {
            EnsureArg.IsNotNull(schemaResolver, nameof(schemaResolver));
            EnsureArg.IsNotNull(versionedStores, nameof(versionedStores));
            _schemaResolver = schemaResolver;
            _versionedStores = versionedStores.ToDictionary(x => x.Version);
        }

        public async Task<TStore> GetInstanceAsync(CancellationToken cancellationToken = default)
        {
            SchemaVersion currentVersion = await _schemaResolver.GetCurrentVersionAsync(cancellationToken);
            if (!_versionedStores.TryGetValue(currentVersion, out TVersionedStore value))
            {
                string msg = currentVersion == SchemaVersion.Unknown
                    ? DicomSqlServerResource.UnknownSchemaVersion
                    : DicomSqlServerResource.SchemaVersionOutOfRange;

                throw new KeyNotFoundException(msg);
            }

            return value;
        }
    }
}
