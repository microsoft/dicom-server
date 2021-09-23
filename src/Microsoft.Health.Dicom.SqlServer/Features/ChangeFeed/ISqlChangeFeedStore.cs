// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed
{
    /// <summary>
    ///  Sql version of IChangeFeedStore.
    /// </summary>
    internal interface ISqlChangeFeedStore : IChangeFeedStore, IVersioned
    {
    }
}
