// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class DataStoreException : DicomServerException
{
    public DataStoreException(string message, ushort? failureCode = null)
        : this(message, null, failureCode)
    {
    }

    public DataStoreException(Exception innerException, bool isExternal)
       : this(isExternal ? string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, innerException?.Message) : DicomCoreResource.DataStoreOperationFailed, null, null)
    {
        IsExternal = isExternal;
    }

    public DataStoreException(Exception innerException, ushort? failureCode = null)
        : this(DicomCoreResource.DataStoreOperationFailed, innerException, failureCode)
    {
    }

    public DataStoreException(string message, Exception innerException, ushort? failureCode = null)
        : base(message, innerException)
    {
        FailureCode = failureCode;
    }

    public ushort? FailureCode { get; }

    public bool IsExternal { get; }
}
