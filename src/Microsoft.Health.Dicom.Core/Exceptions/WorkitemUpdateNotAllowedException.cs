// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions;

public sealed class WorkitemUpdateNotAllowedException : DicomServerException
{
    public WorkitemUpdateNotAllowedException(string procedureStepState)
        : base(string.Format(DicomCoreResource.WorkitemUpdateIsNotAllowed, procedureStepState))
    {
    }
}
