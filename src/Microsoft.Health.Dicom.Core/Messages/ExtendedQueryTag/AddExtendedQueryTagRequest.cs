// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class AddExtendedQueryTagRequest : IRequest<AddExtendedQueryTagResponse>
    {
        public AddExtendedQueryTagRequest(IEnumerable<ExtendedQueryTagEntry> extendedQueryTags)
        {
            ExtendedQueryTags = extendedQueryTags;
        }

        public IEnumerable<ExtendedQueryTagEntry> ExtendedQueryTags { get; }
    }
}
