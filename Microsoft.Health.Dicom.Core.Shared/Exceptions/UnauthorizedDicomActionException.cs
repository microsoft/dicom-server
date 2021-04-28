// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Shared;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class UnauthorizedDicomActionException : DicomServerException
    {
        public UnauthorizedDicomActionException(DataActions expectedDataActions)
            : base(DicomCoreResource.Forbidden)
        {
            ExpectedDataActions = expectedDataActions;
        }

        public DataActions ExpectedDataActions { get; }
    }
}
