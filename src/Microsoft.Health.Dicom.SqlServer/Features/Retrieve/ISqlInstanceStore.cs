// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Feature.Common;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    /// <summary>
    ///  Sql version of IIndexDataStore.
    /// </summary>
    internal interface ISqlInstanceStore : IInstanceStore, IVersioned
    {
    }
}
