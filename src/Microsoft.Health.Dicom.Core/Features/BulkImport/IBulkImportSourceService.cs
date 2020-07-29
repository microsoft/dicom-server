// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public interface IBulkImportSourceService
    {
        Task EnableBulkImportSourceAsync(string accountName, CancellationToken cancellationToken = default);
    }
}
