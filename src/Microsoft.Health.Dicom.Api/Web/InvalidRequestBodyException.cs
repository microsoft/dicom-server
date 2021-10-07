// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Web
{
    public class InvalidRequestBodyException : ValidationException
    {
        public InvalidRequestBodyException(string key, string errorMessage)
           : base(string.Format(CultureInfo.InvariantCulture, DicomApiResource.InvalidRequestBody, key, errorMessage))
        {
        }
    }
}
