// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal class SqlIndexDataStoreFactory : IIndexDataStoreFactory
    {
        private readonly SchemaInformation _schemaInformation;
        private readonly IEnumerable<ISqlIndexDataStore> _sqlIndexDataStores;

        public SqlIndexDataStoreFactory(SchemaInformation schemaInformation, IEnumerable<ISqlIndexDataStore> sqlIndexDataStores)
        {
            _schemaInformation = schemaInformation;
            _sqlIndexDataStores = sqlIndexDataStores;
        }

        public IIndexDataStore GetInstance()
        {
            return _sqlIndexDataStores.First(item => item.Version == _schemaInformation.Current.Value);
        }
    }
}
