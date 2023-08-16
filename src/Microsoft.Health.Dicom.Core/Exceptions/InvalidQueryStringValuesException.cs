// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Web;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class InvalidQueryStringValuesException : ValidationException
{
    public InvalidQueryStringValuesException(string key, string error)
        : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidQueryStringValue, key, HttpUtility.UrlEncode(error)))
    {
    }
}
