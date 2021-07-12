// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsResponse
    {
        public GetExtendedQueryTagErrorsResponse(IReadOnlyCollection<ExtendedQueryTagError> extendedQueryTagErrors)
        {
            ExtendedQueryTagErrors = EnsureArg.IsNotNull(extendedQueryTagErrors);
            ErrorCount = extendedQueryTagErrors.Count;
        }

        public int ErrorCount { get; }

        public IReadOnlyCollection<ExtendedQueryTagError> ExtendedQueryTagErrors { get; }
    }
}
