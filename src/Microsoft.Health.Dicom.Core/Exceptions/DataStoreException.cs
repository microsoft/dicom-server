// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class DataStoreException : DicomServerException
{
    public DataStoreException(Exception innerException, bool isExternal = false)
       : this(isExternal ? string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, innerException?.Message) : DicomCoreResource.DataStoreOperationFailed, null, isExternal)
    {
    }

    public DataStoreException(string message, ushort? failureCode = null, bool isExternal = false)
       : base(message)
    {
        FailureCode = failureCode;
        IsExternal = isExternal;
    }

    public ushort? FailureCode { get; }

    public bool IsExternal { get; }
}
