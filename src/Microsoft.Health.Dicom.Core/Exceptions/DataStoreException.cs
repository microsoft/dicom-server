// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class DataStoreException : ConditionalExternalException
{
    public DataStoreException(Exception exception, bool isExternal = false) : this(isExternal ? string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, (exception?.InnerException is null ? exception.Message : exception.InnerException.Message)) : DicomCoreResource.DataStoreOperationFailed, exception, null, isExternal)
    {
    }

    public DataStoreException(string message, ushort? failureCode = null, bool isExternal = false)
       : this(message, null, failureCode, isExternal)
    {
    }

    public DataStoreException(string message, Exception innerException, ushort? failureCode = null, bool isExternal = false)
       : base(message, innerException, isExternal)
    {
        FailureCode = failureCode;
    }

    public ushort? FailureCode { get; }
}
