// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Azure;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class DataStoreRequestFailedException : ConditionalExternalException
{
    public DataStoreRequestFailedException(RequestFailedException ex, bool isExternal = false)
        : base(
            (isExternal ?
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, ex?.ErrorCode)
                : DicomCoreResource.DataStoreOperationFailed),
            ex,
            isExternal)
    {
    }
}