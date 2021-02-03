// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Exceptions
{
    public class DicomTagException : Exception
    {
        public DicomTagException(string message)
            : base(message)
        {
        }
    }
}
