// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Validate if given custom tag entries are valid
    /// </summary>
    public interface ICustomTagEntryFormalizer
    {
        CustomTagEntry Formalize(CustomTagEntry customTagEntry);
    }
}
