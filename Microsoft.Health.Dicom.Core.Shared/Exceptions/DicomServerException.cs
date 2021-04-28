// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Base class for all server exceptions
    /// </summary>
    public abstract class DicomServerException : MicrosoftHealthException
    {
        protected DicomServerException(string message)
            : base(message)
        {
        }

        protected DicomServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
