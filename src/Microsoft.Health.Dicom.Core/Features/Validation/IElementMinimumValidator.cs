// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Minimum validator on Dicom Element
    /// </summary>
    public interface IElementMinimumValidator
    {
        /// <summary>
        /// Validate Dicom Element.
        /// </summary>
        /// <param name="dicomElement">The Dicom Element</param>
        /// <exception cref="DicomElementValidationException"/>
        void Validate(DicomElement dicomElement);
    }
}
