// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Security.Authorization;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public abstract class BaseHandler
    {
        protected BaseHandler(IDicomAuthorizationService dicomAuthorizationService)
        {
            AuthorizationService = EnsureArg.IsNotNull(dicomAuthorizationService, nameof(dicomAuthorizationService));
        }

        public IDicomAuthorizationService AuthorizationService { get; }
    }
}
