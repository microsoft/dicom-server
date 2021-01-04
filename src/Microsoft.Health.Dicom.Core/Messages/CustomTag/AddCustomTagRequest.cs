// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class AddCustomTagRequest : IRequest<AddCustomTagResponse>
    {
        public AddCustomTagRequest(IEnumerable<CustomTagEntry> customTags)
        {
            CustomTags = customTags;
        }

        public IEnumerable<CustomTagEntry> CustomTags { get; }
    }
}
