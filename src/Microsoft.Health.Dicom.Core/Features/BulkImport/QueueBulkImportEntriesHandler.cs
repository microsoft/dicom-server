// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.BulkImport;

namespace Microsoft.Health.Dicom.Core.Features.BulkImport
{
    public class QueueBulkImportEntriesHandler : IRequestHandler<QueueBulkImportEntriesRequest, QueueBulkImportEntriesResponse>
    {
        private readonly IBulkImportService _bulkImportService;

        public QueueBulkImportEntriesHandler(IBulkImportService bulkImportService)
        {
            _bulkImportService = bulkImportService;
        }

        public async Task<QueueBulkImportEntriesResponse> Handle(QueueBulkImportEntriesRequest request, CancellationToken cancellationToken)
        {
            await _bulkImportService.QueueBulkImportEntriesAsync(request.AccountName, request.BlobReferences, cancellationToken);

            return new QueueBulkImportEntriesResponse();
        }
    }
}
