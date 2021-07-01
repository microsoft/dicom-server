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
            // if the service is starting without schema initialized
            if (_schemaInformation.Current == null)
            {
                return _indexDataStores.First(store => (int)store.Version == _schemaInformation.MinimumSupportedVersion);
            }

            return _indexDataStores.FirstOrDefault(store => (int)store.Version == _schemaInformation.Current);
        }
    }
}
