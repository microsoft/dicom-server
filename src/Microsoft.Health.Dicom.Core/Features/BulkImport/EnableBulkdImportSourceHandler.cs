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
    public class EnableBulkdImportSourceHandler : IRequestHandler<EnableBulkImportSourceRequest, EnableBulkImportSourceResponse>
    {
        private readonly IBulkImportSourceService _bulkImportSourceService;

        public EnableBulkdImportSourceHandler(IBulkImportSourceService bulkImportSourceService)
        {
            _bulkImportSourceService = bulkImportSourceService;
        }

        public async Task<EnableBulkImportSourceResponse> Handle(EnableBulkImportSourceRequest request, CancellationToken cancellationToken)
        {
            await _bulkImportSourceService.EnableBulkImportSourceAsync(request.AccountName, cancellationToken);

            return new EnableBulkImportSourceResponse();
        }
    }
}
