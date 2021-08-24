// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class AcknowledgeTagErrorResponse
    {
        public AcknowledgeTagErrorResponse(ExtendedQueryTagError tagError)
        {
            TagError = EnsureArg.IsNotNull(tagError, nameof(tagError));
        }

        public ExtendedQueryTagError TagError { get; }
    }
}
