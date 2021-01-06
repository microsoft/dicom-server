// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class CustomTagEntryValidationException : ValidationException
    {
        public CustomTagEntryValidationException(string message)
            : base(message)
        {
        }

        public CustomTagEntryValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
