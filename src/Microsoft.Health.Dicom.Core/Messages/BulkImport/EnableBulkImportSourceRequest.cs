// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.BulkImport
{
    public class EnableBulkImportSourceRequest : IRequest<EnableBulkImportSourceResponse>
    {
        public EnableBulkImportSourceRequest(string accountName)
        {
            AccountName = accountName;
        }

        public string AccountName { get; }
    }
}
