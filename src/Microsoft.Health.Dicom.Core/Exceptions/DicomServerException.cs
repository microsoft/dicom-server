// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Base class for all server exceptions
    /// </summary>
    public abstract class DicomServerException : Exception
    {
        public DicomServerException(string message)
            : base(message)
        {
        }
    }
}
