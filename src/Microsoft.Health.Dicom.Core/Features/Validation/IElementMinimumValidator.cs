// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// Minimum validator on Dicom Element
/// </summary>
public interface IElementMinimumValidator
{
    /// <summary>
    /// Validate Dicom Element.
    /// </summary>
    /// <param name="dicomElement">The Dicom Element</param>
    /// <param name="withLeniency">Whether or not to validate with additional leniency</param>
    /// <exception cref="ElementValidationException"/>
    void Validate(DicomElement dicomElement, bool withLeniency = false);
}
