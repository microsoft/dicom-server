// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    ///  Sql version of IIndexDataStore.
    /// </summary>
    internal interface ISqlIndexDataStore : IIndexDataStore, IVersioned
    {
    }
}
