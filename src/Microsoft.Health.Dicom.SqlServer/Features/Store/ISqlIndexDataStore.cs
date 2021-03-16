// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal interface ISqlIndexDataStore : IIndexDataStore
    {
        int Version { get; }
    }
}
