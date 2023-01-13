// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Store;

internal sealed class StoreValidationResultBuilder
{
    private readonly List<string> _warningMessages;
    private readonly Dictionary<ErrorTag, string> _invalidDicomTagErrors;

    // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
    private ValidationWarnings _warningCodes;

    // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
    private readonly Exception _firstException;

    public StoreValidationResultBuilder()
    {
        _warningMessages = new List<string>();
        _invalidDicomTagErrors = new Dictionary<ErrorTag, string>();

        // TODO: Remove these during the cleanup. (this is to support the existing validator behavior)
        _warningCodes = ValidationWarnings.None;
        _firstException = null;
    }

    public StoreValidationResult Build()
    {
        return new StoreValidationResult(
            _warningMessages,
            _warningCodes,
            _firstException,
            _invalidDicomTagErrors);
    }

    public void Add(Exception ex, DicomTag dicomTag, bool isCoreTag = false)
    {
        // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
        // _firstException ??= ex;

        _invalidDicomTagErrors.TryAdd(new ErrorTag(dicomTag, isCoreTag), GetFormattedText(ex?.Message, dicomTag));
    }

    public void Add(ValidationWarnings warningCode, DicomTag queryTag = null)
    {
        // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
        _warningCodes |= warningCode;

        if (warningCode != ValidationWarnings.None)
        {
            _warningMessages.Add(GetFormattedText(GetWarningMessage(warningCode), queryTag));
        }
    }

    private static string GetFormattedText(string message, DicomTag dicomTag = null)
    {
        EnsureArg.IsNotNull(message, nameof(message));

        if (dicomTag == null)
            return message;

        return $"{dicomTag} - {message}";
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
