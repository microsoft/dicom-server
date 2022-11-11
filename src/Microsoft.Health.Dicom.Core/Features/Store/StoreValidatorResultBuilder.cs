// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Store;

internal sealed class StoreValidatorResultBuilder
{
    public StoreValidatorResultBuilder()
    {
        ErrorMessages = new Collection<string>();
        WarningMessages = new Collection<string>();
        WarningCodes = ValidationWarnings.None;
        FirstException = null;
    }

    private Collection<string> ErrorMessages { get; }

    private Collection<string> WarningMessages { get; }

    private ValidationWarnings WarningCodes { get; set; }

    // TODO: Remove this during the cleanup. *** Hack to support the existing validator behavior ***
    private Exception FirstException { get; set; }

    public StoreValidatorResult Build()
    {
        return new StoreValidatorResult(
            ErrorMessages,
            WarningMessages,
            WarningCodes,
            FirstException);
    }

    public void AddError(Exception ex, QueryTag queryTag = null)
    {
        // TODO: Remove this during the cleanup. *** Hack to support the existing validator behavior ***
        if (null == FirstException)
        {
            FirstException = ex;
        }

        ErrorMessages.Add(GetFormattedText(ex?.Message, queryTag));
    }

    public void AddError(string message, QueryTag queryTag = null)
    {
        if (queryTag == null || string.IsNullOrWhiteSpace(message))
            return;

        ErrorMessages.Add(GetFormattedText(message, queryTag));
    }

    public void AddWarning(ValidationWarnings warningCode, QueryTag queryTag = null)
    {
        if (queryTag == null || warningCode == ValidationWarnings.None)
            return;

        WarningCodes |= warningCode;

        WarningMessages.Add(GetFormattedText(GetWarningMessage(warningCode), queryTag));
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
