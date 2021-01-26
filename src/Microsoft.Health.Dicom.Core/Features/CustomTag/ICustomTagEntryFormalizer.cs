// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Formalize custom tag entry.
    /// </summary>
    public interface ICustomTagEntryFormalizer
    {
        /// <summary>
        /// Formalize custom tag entry before saving to CustomTagStore.
        /// </summary>
        /// <param name="customTagEntry">The custom tag entry.</param>
        /// <returns>Formalized custom tag entry.</returns>
        CustomTagEntry Formalize(CustomTagEntry customTagEntry);
    }
}
