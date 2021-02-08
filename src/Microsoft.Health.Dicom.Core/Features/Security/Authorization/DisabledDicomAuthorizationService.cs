// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Security.Authorization
{
    public class DisabledDicomAuthorizationService : IDicomAuthorizationService
    {
        public ValueTask<DataActions> CheckAccess(DataActions dataActions, CancellationToken cancellationToken)
        {
            return new ValueTask<DataActions>(dataActions);
        }
    }
}
