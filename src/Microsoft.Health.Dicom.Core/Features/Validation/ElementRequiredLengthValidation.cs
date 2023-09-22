// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class ElementRequiredLengthValidation : IElementValidation
{
    private static readonly HashSet<DicomVR> StringVrs = new HashSet<DicomVR>()
    {
       DicomVR.AE,
       DicomVR.AS,
       DicomVR.CS,
       DicomVR.DA,
       DicomVR.DS,
       DicomVR.IS,
       DicomVR.LO,
       DicomVR.PN,
       DicomVR.SH,
       DicomVR.UI,
    };

    public int ExpectedLength { get; }

    public ElementRequiredLengthValidation(int expectedLength)
    {
        Debug.Assert(expectedLength >= 0, "Expected Length should be none-negative");
        ExpectedLength = expectedLength;
    }

    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        DicomVR vr = dicomElement.ValueRepresentation;
        if (TryGetAsString(dicomElement, out string value))
        {
            if (withLeniency)
            {
                value = value.TrimEnd('\0');
            }
            ValidateStringLength(vr, dicomElement.Tag.GetFriendlyName(), value);
        }
        else
        {
            ValidateByteBufferLength(vr, dicomElement.Tag.GetFriendlyName(), dicomElement.Buffer);
        }
    }

    private void ValidateByteBufferLength(DicomVR dicomVR, string name, IByteBuffer value)
    {
        // We only validate first value, as long as long value.Size>=ExpectedLength, we are good to go.
        if (value == null || value.Size == 0 || value.Size < ExpectedLength)
        {
            throw new ElementValidationException(
                name,
                dicomVR,
                ValidationErrorCode.UnexpectedLength,
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, ExpectedLength));
        }
    }

    private static bool TryGetAsString(DicomElement dicomElement, out string value)
    {
        value = string.Empty;
        if (StringVrs.Contains(dicomElement.ValueRepresentation))
        {
            // Only validate the first element
            value = dicomElement.GetFirstValueOrDefault<string>();
            return true;
        }

        return false;
    }

    private void ValidateStringLength(DicomVR dicomVR, string name, string value)
    {
        value ??= "";
        if (value.Length != ExpectedLength)
        {
            throw new ElementValidationException(
                name,
                dicomVR,
                ValidationErrorCode.UnexpectedLength,
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, ExpectedLength));
        }
    }
}
