// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Delete
{
    public class DicomDeleteService : IDicomDeleteService
    {
        public Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken)
        {
            // TODO Not throwing to keep RetrieveTransactionEnETests that are cleaning up happy
            return Task.CompletedTask;
        }

        public Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
