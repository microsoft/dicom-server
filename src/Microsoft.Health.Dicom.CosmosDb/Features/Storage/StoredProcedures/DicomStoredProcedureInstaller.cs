// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.CosmosDb.Features.Storage.StoredProcedures;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Versioning;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.StoredProcedures
{
    public class DicomStoredProcedureInstaller : StoredProcedureInstaller, IDicomCollectionUpdater
    {
        private readonly IEnumerable<IDicomStoredProcedure> _storedProcedures;

        public DicomStoredProcedureInstaller(IEnumerable<IDicomStoredProcedure> storedProcedures)
        {
            EnsureArg.IsNotNull(storedProcedures, nameof(storedProcedures));

            _storedProcedures = storedProcedures;
        }

        public override IEnumerable<IStoredProcedure> GetStoredProcedures()
        {
            return _storedProcedures;
        }
    }
}
