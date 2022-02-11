// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    /// <summary>
    ///  Sql version of IWorkitemStore.
    /// </summary>
    internal interface ISqlWorkitemStore : IIndexWorkitemStore, IVersioned
    {
    }
}
