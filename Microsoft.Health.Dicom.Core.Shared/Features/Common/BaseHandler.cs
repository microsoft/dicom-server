// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Features.Security;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public abstract class BaseHandler
    {
        protected BaseHandler(IAuthorizationService<DataActions> authorizationService)
        {
            AuthorizationService = EnsureArg.IsNotNull(authorizationService, nameof(authorizationService));
        }

        public IAuthorizationService<DataActions> AuthorizationService { get; }
    }
}
