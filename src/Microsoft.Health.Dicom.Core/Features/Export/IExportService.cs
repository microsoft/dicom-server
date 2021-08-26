// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Export
{
    public interface IExportService
    {
        Task Export(
            IReadOnlyCollection<string> instances,
            string cohortId,
            string destinationBlobConnectionString,
            string destinationBlobContainerName,
            string contentType = KnownContentTypes.ApplicationDicom,
            CancellationToken cancellationToken = default);
    }
}
