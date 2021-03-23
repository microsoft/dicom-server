// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class DeleteExtendedQueryTagRequest : IRequest<DeleteExtendedQueryTagResponse>
    {
        public DeleteExtendedQueryTagRequest(string tagPath)
        {
            TagPath = tagPath;
        }

        public string TagPath { get; }
    }
}
