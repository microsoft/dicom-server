// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the extended query tags exceeds max allowed count.
    /// </summary>
    public class ExtendedQueryTagsExceedsMaxAllowedCountException : DicomServerException
    {
        public ExtendedQueryTagsExceedsMaxAllowedCountException(int maxAllowedCount)
            : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExtendedQueryTagsExceedsMaxAllowedCount, maxAllowedCount))
        {
        }
    }
}
