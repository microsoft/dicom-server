// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;

namespace Microsoft.Health.Dicom.Core.Features.ADX
{
    public interface IADXService
    {
        DataTable ExecuteQueryAsync(string queryText);
    }
}
