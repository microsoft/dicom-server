// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Store;

internal sealed class StoreValidationResultBuilder
{
    private readonly List<string> _errorMessages;
    private readonly List<string> _warningMessages;

    // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
    private ValidationWarnings _warningCodes;

    // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
    private Exception _firstException;

    public StoreValidationResultBuilder()
    {
        _errorMessages = new List<string>();
        _warningMessages = new List<string>();

        // TODO: Remove these during the cleanup. (this is to support the existing validator behavior)
        _warningCodes = ValidationWarnings.None;
        _firstException = null;
    }

    public StoreValidationResult Build()
    {
        return new StoreValidationResult(
            _errorMessages,
            _warningMessages,
            _warningCodes,
            _firstException);
    }

    public void Add(Exception ex, QueryTag queryTag = null)
    {
        // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
        _firstException ??= ex;

        _errorMessages.Add(GetFormattedText(ex?.Message, queryTag));
    }

    public void Add(ValidationWarnings warningCode, QueryTag queryTag = null)
    {
        // TODO: Remove this during the cleanup. (this is to support the existing validator behavior)
        _warningCodes |= warningCode;

        if (warningCode != ValidationWarnings.None)
        {
            _warningMessages.Add(GetFormattedText(GetWarningMessage(warningCode), queryTag));
        }
    }

    private static string GetFormattedText(string message, QueryTag queryTag = null)
    {
        EnsureArg.IsNotNull(message, nameof(message));

        if (queryTag == null)
            return message;

        return $"{queryTag} - {message}";
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
