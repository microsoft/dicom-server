// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Store;

internal sealed class ValidationResultBuilder : IValidationResultBuilder
{
    private readonly IList<string> _errorMessages;
    private readonly IList<string> _warningMessages;

    public ValidationResultBuilder()
    {
        _errorMessages = new List<string>();
        _warningMessages = new List<string>();
        WarningCodes = ValidationWarnings.None;
    }

    public bool HasWarnings
    {
        get { return _warningMessages.Count > 0; }
    }

    public bool HasErrors
    {
        get { return _errorMessages.Count > 0; }
    }

    public IEnumerable<string> Errors
    {
        get { return _errorMessages; }
    }

    public IEnumerable<string> Warnings
    {
        get { return _warningMessages; }
    }

    public ValidationWarnings WarningCodes
    {
        get; private set;
    }

    // TODO: Remove this during the cleanup. *** Hack to support the existing validator behavior ***
    public Exception FirstException { get; private set; }

    public void AddError(Exception ex, QueryTag queryTag = null)
    {
        // TODO: Remove this during the cleanup. *** Hack to support the existing validator behavior ***
        if (null == FirstException)
        {
            FirstException = ex;
        }

        _errorMessages.Add(GetFormattedText(ex?.Message, queryTag));
    }

    public void AddError(string message, QueryTag queryTag = null)
    {
        if (queryTag == null || string.IsNullOrWhiteSpace(message))
            return;

        _errorMessages.Add(GetFormattedText(message, queryTag));
    }

    public void AddWarning(ValidationWarnings warningCode, QueryTag queryTag = null)
    {
        if (queryTag == null || warningCode == ValidationWarnings.None)
            return;

        WarningCodes |= warningCode;

        _warningMessages.Add(GetFormattedText(GetWarningMessage(warningCode), queryTag));
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
        switch (warningCode)
        {
            case ValidationWarnings.IndexedDicomTagHasMultipleValues:
                return DicomCoreResource.ErrorMessageMultiValues;
            case ValidationWarnings.DatasetDoesNotMatchSOPClass:
                return DicomCoreResource.DatasetDoesNotMatchSOPClass;
        }

        return string.Empty;
    }
}
