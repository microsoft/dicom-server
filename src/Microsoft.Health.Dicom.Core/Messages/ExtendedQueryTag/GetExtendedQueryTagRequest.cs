// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagRequest : IRequest<GetExtendedQueryTagResponse>
    {
        public GetExtendedQueryTagRequest(string extendedQueryTagPath)
        {
            ExtendedQueryTagPath = extendedQueryTagPath;
        }

        /// <summary>
        /// Path for the extended query tag that is requested.
        /// </summary>
        public string ExtendedQueryTagPath { get; }
    }
}
