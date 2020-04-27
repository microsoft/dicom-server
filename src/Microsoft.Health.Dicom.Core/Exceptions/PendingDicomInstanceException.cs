// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when there is already a pending DICOM instance being created.
    /// </summary>
    public class PendingDicomInstanceException : Exception
    {
    }
}
