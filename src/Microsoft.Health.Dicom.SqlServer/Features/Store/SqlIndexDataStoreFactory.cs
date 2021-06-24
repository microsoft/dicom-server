// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal class SqlIndexDataStoreFactory : IIndexDataStoreFactory
    {
        private readonly SchemaInformation _schemaInformation;
        private readonly IEnumerable<ISqlIndexDataStore> _indexDataStores;

        public SqlIndexDataStoreFactory(SchemaInformation schemaInformation, IEnumerable<ISqlIndexDataStore> sqlIndexDataStores)
        {
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(sqlIndexDataStores, nameof(sqlIndexDataStores));
            _schemaInformation = schemaInformation;
            _indexDataStores = sqlIndexDataStores;
        }

        public IIndexDataStore GetInstance()
        {
            int currentVersion = _schemaInformation.Current == null ? 0 : _schemaInformation.Current.Value;
            return _indexDataStores.FirstOrDefault(store => (int)store.Version == currentVersion);
        }
    }
}
