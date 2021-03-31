// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception that is thrown when extended query tag feature is disabled.
    /// </summary>
    public class ExtendedQueryTagFeatureDisabledException : BadRequestException
    {
        public ExtendedQueryTagFeatureDisabledException()
            : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExtendedQueryTagFeatureDisabled))
        {
        }
    }
}
