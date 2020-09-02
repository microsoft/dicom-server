// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class InvalidQueryStringValuesException : ValidationException
    {
        public InvalidQueryStringValuesException(string firstInvalidParameter)
            : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidQueryStringValue, firstInvalidParameter))
        {
        }
    }
}
