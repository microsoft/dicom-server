// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class GetCustomTagRequest : IRequest<GetCustomTagResponse>
    {
        public GetCustomTagRequest(string customTagPath)
        {
            CustomTagPath = customTagPath;
        }

        /// <summary>
        /// Path for the custom tag that is requested. Value is null when request is to get all custom tags.
        /// </summary>
        public string CustomTagPath { get; }
    }
}
