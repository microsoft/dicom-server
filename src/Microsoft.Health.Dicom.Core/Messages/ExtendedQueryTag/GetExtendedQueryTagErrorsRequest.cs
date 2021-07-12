// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsRequest : IRequest<GetExtendedQueryTagErrorsResponse>
    {
        public GetExtendedQueryTagErrorsRequest(string path)
        {
            EnsureArg.IsNotNullOrWhiteSpace(path, nameof(path));
            Path = path;
        }

        public string Path { get; }
    }
}
