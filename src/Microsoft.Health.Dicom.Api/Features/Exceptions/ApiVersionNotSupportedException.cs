// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Features.Exceptions;

public class ApiVersionNotSupportedException : BadRequestException
{
    public ApiVersionNotSupportedException(int version)
        : base(string.Format(CultureInfo.InvariantCulture, DicomApiResource.ApiVersionNotSupported, version))
    {
    }
}
