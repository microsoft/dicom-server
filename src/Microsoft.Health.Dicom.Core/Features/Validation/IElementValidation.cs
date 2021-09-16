// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validation on Dicom Element.
    /// </summary>
    internal interface IElementValidation
    {
        /// <summary>
        /// Validate DicomElement
        /// </summary>
        /// <param name="dicomElement">The dicom element</param>
        /// <exception cref="ElementValidation"/>
        void Validate(DicomElement dicomElement);
    }
}
