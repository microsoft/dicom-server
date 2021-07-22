// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    /// <summary>
    ///  Sql version of IInstanceStore.
    /// </summary>
    internal interface ISqlInstanceStore : IInstanceStore, IVersioned
    {
    }
}
