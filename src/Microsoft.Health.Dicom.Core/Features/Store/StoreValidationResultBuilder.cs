// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Store;

internal sealed class StoreValidationResultBuilder
{
    private readonly List<string> _warningMessages;
    private readonly Dictionary<DicomTag, StoreErrorResult> _invalidDicomTagErrors;

    // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
    private ValidationWarnings _warningCodes;

    public StoreValidationResultBuilder()
    {
        _warningMessages = new List<string>();
        _invalidDicomTagErrors = new Dictionary<DicomTag, StoreErrorResult>();

        // TODO: Remove these during the cleanup. (this is to support the existing validator behavior)
        _warningCodes = ValidationWarnings.None;
    }

    public StoreValidationResult Build()
    {
        return new StoreValidationResult(
            _warningMessages,
            _warningCodes,
            _invalidDicomTagErrors);
    }

    public void Add(Exception ex, DicomTag dicomTag, bool isCoreTag = false)
    {
        var errorResult = new StoreErrorResult(GetFormattedText(ex?.Message, dicomTag), isCoreTag);
        _invalidDicomTagErrors.TryAdd(dicomTag, errorResult);
    }

    public void Add(ValidationWarnings warningCode, DicomTag dicomTag = null)
    {
        // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
        _warningCodes |= warningCode;

        if (warningCode != ValidationWarnings.None)
        {
            _warningMessages.Add(GetFormattedText(GetWarningMessage(warningCode), dicomTag));
        }
    }

    private static string GetFormattedText(string message, DicomTag dicomTag = null)
    {
        EnsureArg.IsNotNull(message, nameof(message));

        if (dicomTag == null)
            return message;

        return string.Format(
            CultureInfo.InvariantCulture,
            DicomCoreResource.ErrorMessageFormat,
            ErrorNumbers.ValidationFailure,
            dicomTag.ToString(),
            message);
    }

    private static string GetWarningMessage(ValidationWarnings warningCode)
    {
        return warningCode switch
        {
            ValidationWarnings.IndexedDicomTagHasMultipleValues => DicomCoreResource.ErrorMessageMultiValues,
            ValidationWarnings.DatasetDoesNotMatchSOPClass => DicomCoreResource.DatasetDoesNotMatchSOPClass,
            _ => string.Empty,
        };
    }
}
