// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class ElementRequiredLengthValidation : StringElementValidation
{
    protected override bool AllowNullOrEmpty => false;

    private static readonly HashSet<DicomVR> StringVrs = new()
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

    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
    {
        if (!String.IsNullOrEmpty(value))
        {
            ValidateStringLength(vr, name, value);
        }
        else
        {
            ValidateByteBufferLength(vr, name, buffer);
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

    protected override string GetValueOrDefault(DicomElement dicomElement)
    {
        if (StringVrs.Contains(dicomElement.ValueRepresentation))
        {
            // Only validate the first element
            return dicomElement.GetFirstValueOrDefault<string>();
        }
        return string.Empty;
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
