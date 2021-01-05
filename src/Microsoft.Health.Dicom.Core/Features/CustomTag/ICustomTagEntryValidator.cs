// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Validate if given custom tag entries are valid
    /// </summary>
    public interface ICustomTagEntryValidator
    {
        /// <summary>
        /// Validate if given custom tag ehtries are valid.
        /// </summary>
        /// <param name="customTagEntries">The custom tag entries</param>
        void ValidateCustomTags(IEnumerable<CustomTagEntry> customTagEntries);
    }
}
