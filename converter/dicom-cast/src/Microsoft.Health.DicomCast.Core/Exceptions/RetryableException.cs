// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Exceptions
{
    public class RetryableException : Exception
    {
        protected RetryableException()
        {
        }

        protected RetryableException(string message)
            : base(message)
        {
        }

        protected RetryableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ChangeFeedEntry ChangeFeedEntry { get; set; }
    }
}
