// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Validate if given extended query tag entries are valid
    /// </summary>
    public interface IExtendedQueryTagEntryValidator
    {
        /// <summary>
        /// Validate if given extended query tag entries are valid.
        /// </summary>
        /// <param name="extendedQueryTagEntries">The extended query tag entries</param>
        void ValidateExtendedQueryTags(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries);
    }
}
