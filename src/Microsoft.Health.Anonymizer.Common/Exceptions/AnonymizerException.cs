// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Anonymizer.Common.Exceptions
{
    public class AnonymizerException : Exception
    {
        public AnonymizerException(AnonymizerErrorCode errorCode, string message)
            : base(message)
        {
            AnonymizerErrorCode = errorCode;
        }

        public AnonymizerException(AnonymizerErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            AnonymizerErrorCode = errorCode;
        }

        public AnonymizerErrorCode AnonymizerErrorCode { get; }
    }
}
