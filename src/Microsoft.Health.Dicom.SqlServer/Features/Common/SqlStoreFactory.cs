// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Feature.Common;
using Microsoft.Health.SqlServer.Features.Schema;

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
        private readonly SchemaInformation _schemaInformation;
        private readonly IEnumerable<TVersionedStore> _sqlVersioningStores;

        public SqlStoreFactory(SchemaInformation schemaInformation, IEnumerable<TVersionedStore> versionedStores)
        {
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(versionedStores, nameof(versionedStores));
            _schemaInformation = schemaInformation;
            _sqlVersioningStores = versionedStores;
        }

        public TStore GetInstance()
        {
            return _sqlVersioningStores.First(store => (int)store.Version == _schemaInformation.Current.Value);
        }
    }
}
