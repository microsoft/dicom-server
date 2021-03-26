// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class ExtendedQueryTagEntryValidationException : ValidationException
    {
        public ExtendedQueryTagEntryValidationException(string message)
            : base(message)
        {
        }

        public ExtendedQueryTagEntryValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
