// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// Validation on Dicom Element.
/// </summary>
internal interface IElementValidation
{
    /// <summary>
    /// Validate DicomElement
    /// </summary>
    /// <param name="dicomElement">The dicom element</param>
    /// <param name="validationLevel">Validate with specific style - strict or more leninent/default</param>
    /// <exception cref="ElementValidationException"/>
    void Validate(DicomElement dicomElement, ValidationLevel validationLevel = ValidationLevel.Strict);
}
