// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Azure;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class DataStoreRequestFailedException : ConditionalExternalException
{
    public int ResponseCode => InnerException is RequestFailedException ex ? ex.Status : 0;

    public string ErrorCode => InnerException is RequestFailedException ex ? ex.ErrorCode : null;

    public DataStoreRequestFailedException(RequestFailedException ex, bool isExternal = false)
        : base(
            (isExternal ?
                getFormattedExternalStoreMessage(ex)
                : DicomCoreResource.DataStoreOperationFailed),
            ex,
            isExternal)
    {
    }

    private static string getFormattedExternalStoreMessage(RequestFailedException ex)
    {
        return !string.IsNullOrEmpty(ex?.ErrorCode)
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.ExternalDataStoreOperationFailed,
                    ex?.ErrorCode)
                : GetFormattedExternalStoreMessageWithoutErrorCode(ex)
            ;
    }

    private static string GetFormattedExternalStoreMessageWithoutErrorCode(RequestFailedException ex)
    {
        if (ex.IsStorageAccountUnknownHostError())
        {
            return DicomCoreResource.ExternalDataStoreHostIsUnknown;
        }

        // if we do not have an error code and internal message is not "host not known", we are not familiar with the issue
        // we can't just give back the exception message as it may contain sensitive information
        return DicomCoreResource.ExternalDataStoreOperationFailedUnknownIssue;
    }
}
